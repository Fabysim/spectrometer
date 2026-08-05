using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Entretien.Services;
using Spectrometre.Modules.PostesRecrutement.Services;
using Spectrometre.Modules.ProfilCandidat.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Vérifie que <c>IEntretienService.GenererGrilleAsync</c> ne prend aucun raccourci d'accès : il doit
/// refuser (retourner <c>null</c>) exactement dans les mêmes cas que
/// <c>ICompatibiliteService.GetResultatAutorisePourUtilisateurAsync</c> (voir
/// <see cref="CompatibiliteAccessControlTests"/>), puisqu'il délègue entièrement le contrôle d'accès à
/// cet accesseur — jamais d'accès direct par <c>candidateProfileId</c>/<c>companyId</c> bruts.
/// </summary>
[Collection("Base de données partagée")]
public sealed class EntretienAccessControlTests(ServiceFixture fixture)
{
    private IServiceScope NewScope() => fixture.Services.CreateScope();

    [Fact]
    public async Task LeManagerAyantUneCandidatureReelle_ObtientUneGrille()
    {
        var suffix = Guid.NewGuid();
        var candidatUserId = $"entretien-test-candidat-{suffix}";
        var managerUserId = $"entretien-test-manager-{suffix}";

        var company = await fixture.CreateCompanyAsync($"Entreprise Entretien {suffix}", managerUserId);

        using (var setupScope = NewScope())
        {
            var candidateProfileId = await setupScope.ServiceProvider.GetRequiredService<ICandidateProfileService>()
                .GetOrCreateProfileIdAsync(candidatUserId);

            var tenantContext = setupScope.ServiceProvider.GetRequiredService<ITenantContext>();
            tenantContext.SetActiveCompany(company.Id, company.SchemaName);
            var posteService = setupScope.ServiceProvider.GetRequiredService<IPosteService>();
            var posteId = await posteService.CreatePosteAsync($"Poste de test {suffix}", null, null);
            await posteService.PostulerAsync(company.Id, posteId, candidateProfileId);

            using var scope = NewScope();
            var entretienService = scope.ServiceProvider.GetRequiredService<IEntretienService>();

            var grille = await entretienService.GenererGrilleAsync(candidateProfileId, managerUserId);

            Assert.NotNull(grille);
            Assert.Equal(candidateProfileId, grille!.CandidateProfileId);
            // Aucune grille K renseignée côté entreprise pour ce test → tous les axes retombent au score
            // neutre (50%), sous le seuil par défaut (60%) : au moins un groupe de questions doit être généré.
            Assert.NotEmpty(grille.Groupes);
        }
    }

    [Fact]
    public async Task UnTiersSansCandidatureReelle_NObtientAucuneGrille()
    {
        var suffix = Guid.NewGuid();
        var candidatUserId = $"entretien-test-candidat-jamais-{suffix}";
        var tiersUserId = $"entretien-test-tiers-{suffix}";

        using var setupScope = NewScope();
        var candidateProfileId = await setupScope.ServiceProvider.GetRequiredService<ICandidateProfileService>()
            .GetOrCreateProfileIdAsync(candidatUserId);

        using var scope = NewScope();
        var entretienService = scope.ServiceProvider.GetRequiredService<IEntretienService>();

        // tiersUserId ne gère aucune entreprise et n'est pas le candidat : exactement le scénario de la
        // faille corrigée sur /compatibilite/resultat/{id}, rejoué ici sur /entretien/{id}.
        var grille = await entretienService.GenererGrilleAsync(candidateProfileId, tiersUserId);

        Assert.Null(grille);
    }
}
