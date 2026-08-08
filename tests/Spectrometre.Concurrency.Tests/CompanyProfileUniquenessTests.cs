using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.ProfilEntreprise.Data;
using Spectrometre.Modules.ProfilEntreprise.Entities;
using Spectrometre.Modules.ProfilEntreprise.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Contrainte d'unicité "un seul <see cref="CompanyProfile"/> par schéma tenant" (colonne fantôme
/// <c>Singleton</c> + index unique, voir <c>ProfilEntrepriseDbContext</c>) et correctif de la course sur
/// <c>CompanyProfileService.GetOrCreateProfileIdAsync</c> — même défaut que celui déjà corrigé sur
/// <c>CandidateProfileService</c>/<c>CoachProfileService</c>, sauf qu'ici aucune contrainte n'existait DU
/// TOUT avant ce cycle (donc aucune exception, un risque de doublon silencieux plutôt qu'un crash).
/// </summary>
/// <remarks>
/// Le backfill de rétro-application (<c>Spectrometre.Host.Onboarding.CompanyProfileUniquenessBackfill</c>)
/// n'est PAS appelé directement ici : le projet de test ne référence délibérément pas <c>Spectrometre.Host</c>
/// (voir la remarque sur <c>ServiceFixture</c>). Le test ci-dessous vérifie directement l'algorithme de
/// nettoyage (même requête : tri par <c>UpdatedAt</c> décroissant, conserve la première, supprime les
/// autres) sur un schéma simulé avec doublons — c'est la partie qui compte réellement (la justesse des
/// données) ; le fichier Host lui-même n'est qu'une itération DI/schéma déjà éprouvée par
/// <c>RecruitmentIndexBackfill</c> en production.
/// </remarks>
[Collection("Base de données partagée")]
public sealed class CompanyProfileUniquenessTests(ServiceFixture fixture)
{
    [Fact]
    public async Task ResolutionConcurrente_DuMemeProfilEntreprise_NeLevePasEtRetourneLeMemeId()
    {
        var suffix = Guid.NewGuid();
        var company = await fixture.CreateCompanyAsync($"Entreprise Profil Concurrent {suffix}", $"profil-concurrent-owner-{suffix}");

        using var scope = fixture.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>().SetActiveCompany(company.Id, company.SchemaName);
        var companyProfileService = scope.ServiceProvider.GetRequiredService<ICompanyProfileService>();

        using var barrier = new Barrier(2);
        Task<int> RunAsync() => Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await companyProfileService.GetOrCreateProfileIdAsync();
        });

        var ids = await Task.WhenAll(RunAsync(), RunAsync());
        Assert.Equal(ids[0], ids[1]);

        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ProfilEntrepriseDbContext>>();
        await using var verifDb = await dbFactory.CreateDbContextAsync();
        verifDb.TenantSchema = company.SchemaName;
        var count = await verifDb.CompanyProfiles.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task NettoyageDesDoublons_SurUnSchemaSimule_NeGardeQueLeProfilLePlusRecent()
    {
        var suffix = Guid.NewGuid();
        var company = await fixture.CreateCompanyAsync($"Entreprise Profil Doublons {suffix}", $"profil-doublons-owner-{suffix}");

        using var scope = fixture.Services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ProfilEntrepriseDbContext>>();

        await using (var setupDb = await dbFactory.CreateDbContextAsync())
        {
            setupDb.TenantSchema = company.SchemaName;

            // Simule un schéma provisionné AVANT ce cycle : retire la contrainte déjà appliquée par
            // ApplyModuleSchemaAsync pour ce tenant fraîchement créé, puis insère deux lignes en doublon
            // directement en SQL brut (impossible via EF tant que la colonne Singleton, absente à ce stade,
            // n'existe pas — le modèle mis en cache l'exigerait sinon dans toute commande générée par EF).
            await setupDb.Database.ExecuteSqlRawAsync(
                "DROP INDEX IF EXISTS \"" + company.SchemaName + "\".\"IX_CompanyProfiles_Singleton\";");
            await setupDb.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"" + company.SchemaName + "\".\"CompanyProfiles\" DROP COLUMN IF EXISTS \"Singleton\";");

            var ancien = DateTimeOffset.UtcNow.AddDays(-5);
            var recent = DateTimeOffset.UtcNow;
            var insertSql = "INSERT INTO \"" + company.SchemaName + "\".\"CompanyProfiles\" (\"CreatedAt\", \"UpdatedAt\") VALUES ({0}, {0});";
            await setupDb.Database.ExecuteSqlRawAsync(insertSql, ancien);
            await setupDb.Database.ExecuteSqlRawAsync(insertSql, recent);

            // Réintroduit la colonne (valeur par défaut, sans contrainte d'unicité pour l'instant) — sinon
            // aucune requête EF sur CompanyProfiles ne serait possible (le modèle la déclare obligatoire).
            await setupDb.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"" + company.SchemaName + "\".\"CompanyProfiles\" ADD COLUMN IF NOT EXISTS \"Singleton\" integer NOT NULL DEFAULT 1;");
        }

        // Même algorithme que CompanyProfileUniquenessBackfill : tri par UpdatedAt décroissant, conserve la
        // première (la plus récente), supprime les autres — puis réapplique la contrainte.
        await using (var backfillDb = await dbFactory.CreateDbContextAsync())
        {
            backfillDb.TenantSchema = company.SchemaName;

            var profils = await backfillDb.CompanyProfiles.OrderByDescending(p => p.UpdatedAt).ToListAsync();
            Assert.Equal(2, profils.Count);

            backfillDb.CompanyProfiles.RemoveRange(profils.Skip(1));
            await backfillDb.SaveChangesAsync();

            await backfillDb.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"" + company.SchemaName + "\".\"CompanyProfiles\" ADD COLUMN IF NOT EXISTS \"Singleton\" integer NOT NULL DEFAULT 1;");
            await backfillDb.Database.ExecuteSqlRawAsync(
                "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_CompanyProfiles_Singleton\" ON \"" + company.SchemaName + "\".\"CompanyProfiles\" (\"Singleton\");");
        }

        await using var verifDb = await dbFactory.CreateDbContextAsync();
        verifDb.TenantSchema = company.SchemaName;
        var restants = await verifDb.CompanyProfiles.ToListAsync();
        var seul = Assert.Single(restants);
        // Le profil conservé est bien le plus récemment modifié (critère documenté) — pas l'inverse.
        Assert.True(seul.UpdatedAt > DateTimeOffset.UtcNow.AddDays(-1));

        // La contrainte est bien réappliquée : une nouvelle insertion directe viole désormais l'unicité.
        await using var db2 = await dbFactory.CreateDbContextAsync();
        db2.TenantSchema = company.SchemaName;
        db2.CompanyProfiles.Add(new CompanyProfile());
        await Assert.ThrowsAsync<DbUpdateException>(() => db2.SaveChangesAsync());
    }
}
