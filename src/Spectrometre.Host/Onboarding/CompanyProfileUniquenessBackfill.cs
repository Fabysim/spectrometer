using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Data;
using Spectrometre.Modules.ProfilEntreprise.Data;

namespace Spectrometre.Host.Onboarding;

/// <summary>
/// Rétroactif, sur chaque schéma tenant déjà provisionné : supprime les doublons <c>CompanyProfile</c>
/// s'il y en a (aucun trouvé en développement au moment d'écrire ce backfill — voir le rapport du cycle),
/// puis ajoute la contrainte d'unicité "un seul profil par schéma" introduite dans ce cycle (colonne
/// fantôme <c>Singleton</c> + index unique, voir <c>ProfilEntrepriseDbContext</c>) — une entreprise
/// provisionnée AVANT cette contrainte n'aurait sinon jamais cette protection tant qu'elle ne serait pas
/// re-provisionnée de zéro. Les NOUVEAUX tenants l'obtiennent automatiquement via
/// <c>ITenantSchemaProvisioner.ApplyModuleSchemaAsync</c> (le script généré reflète déjà le modèle à jour).
/// </summary>
/// <remarks>
/// Critère de nettoyage retenu s'il existe plusieurs lignes : on conserve celle avec le
/// <c>UpdatedAt</c> le plus récent (la plus probablement représentative d'une édition réelle par
/// l'entreprise), les autres sont supprimées. Même raisonnement que
/// <c>RecruitmentIndexBackfill</c> pour vivre dans Host (a besoin du DbContext du module ProfilEntreprise) —
/// idempotent, rejouable sans risque à chaque démarrage.
/// </remarks>
internal static partial class CompanyProfileUniquenessBackfill
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var coreDb = services.GetRequiredService<CoreDbContext>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProfilEntrepriseDbContext>>();

        var companies = await coreDb.Companies.AsNoTracking().ToListAsync(cancellationToken);

        foreach (var company in companies)
        {
            if (!ValidSchemaName().IsMatch(company.SchemaName))
                throw new InvalidOperationException("Schéma invalide.");

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            db.TenantSchema = company.SchemaName;

            try
            {
                // Ajoute la colonne AVANT toute requête EF sur CompanyProfiles : le modèle (mis à jour ce
                // cycle) la projette systématiquement, y compris sur un schéma provisionné avant elle — une
                // lecture EF échouerait sinon (colonne absente), même sur un schéma sans aucun doublon.
                // Valeur par défaut seule ici, jamais l'index unique : celui-ci ne peut être ajouté qu'une
                // fois les doublons éventuels supprimés (voir plus bas), sous peine d'échouer lui-même.
                await db.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE \"" + company.SchemaName + "\".\"CompanyProfiles\" ADD COLUMN IF NOT EXISTS \"Singleton\" integer NOT NULL DEFAULT 1;",
                    cancellationToken);
            }
            catch (Npgsql.PostgresException)
            {
                // Module ProfilEntreprise pas encore provisionné pour cette entreprise (table absente) —
                // rien à nettoyer, le futur provisionnement appliquera directement le modèle à jour.
                continue;
            }

            var profiles = await db.CompanyProfiles.OrderByDescending(p => p.UpdatedAt).ToListAsync(cancellationToken);
            if (profiles.Count > 1)
            {
                // Conserve la ligne la plus récemment modifiée (voir la remarque), supprime les autres.
                db.CompanyProfiles.RemoveRange(profiles.Skip(1));
                await db.SaveChangesAsync(cancellationToken);
            }

            await db.Database.ExecuteSqlRawAsync(
                "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_CompanyProfiles_Singleton\" ON \"" + company.SchemaName + "\".\"CompanyProfiles\" (\"Singleton\");",
                cancellationToken);
        }
    }

    [GeneratedRegex("^[a-z][a-z0-9_]{0,62}$")]
    private static partial Regex ValidSchemaName();
}
