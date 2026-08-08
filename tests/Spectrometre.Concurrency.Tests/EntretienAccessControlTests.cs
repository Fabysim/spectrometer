using Spectrometre.Modules.ProfilEntreprise.Services;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Recruitment;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Entretien.Services;
using Spectrometre.Modules.Recrutement.Services;
using Spectrometre.Modules.ProfilCandidat.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Vérifie que <c>IEntretienService.GenererGrilleAsync</c> ne prend aucun raccourci d'accès : il doit
/// refuser (retourner <c>null</c>) exactement dans les mêmes cas que
/// <c>ICompatibiliteService.GetResultatAutorisePourUtilisateurAsync</c> (voir
/// <see cref="CompatibiliteAccessControlTests"/>), puisqu'il délègue entièrement le contrôle d'accès à
/// cet accesseur — jamais d'accès direct par <c>candidateProfileId</c>/<c>companyId</c> bruts.
/// Couvre aussi le flux candidat de <c>/candidat/entretien/{companyId}</c> (activation tenant +
/// <c>ICandidatureExistenceChecker</c> avant génération).
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
        var employeUserId = $"entretien-test-manager-{suffix}";

        var company = await fixture.CreateCompanyAsync($"Entreprise Entretien {suffix}", employeUserId);

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

            var grille = await entretienService.GenererGrilleAsync(candidateProfileId, employeUserId);

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

    /// <summary>
    /// Flux de <c>/candidat/entretien/{companyId}</c> : le candidat a réellement postulé → après
    /// SetActiveCompany, la candidature est reconnue et GenererGrilleAsync expose des
    /// <c>QuestionsPourEntreprise</c> (sens CandidatVersEntreprise).
    /// </summary>
    [Fact]
    public async Task LeCandidatAyantPostule_ObtientSesQuestionsPourEntreprise()
    {
        var suffix = Guid.NewGuid();
        var candidatUserId = $"entretien-candidat-page-{suffix}";
        var employeUserId = $"entretien-candidat-page-mgr-{suffix}";

        var company = await fixture.CreateCompanyAsync($"Entreprise Entretien Candidat {suffix}", employeUserId);

        using var setupScope = NewScope();
        var candidateProfileId = await setupScope.ServiceProvider.GetRequiredService<ICandidateProfileService>()
            .GetOrCreateProfileIdAsync(candidatUserId);

        setupScope.ServiceProvider.GetRequiredService<ITenantContext>()
            .SetActiveCompany(company.Id, company.SchemaName);
        var posteService = setupScope.ServiceProvider.GetRequiredService<IPosteService>();
        var posteId = await posteService.CreatePosteAsync($"Poste entretien candidat {suffix}", null, null);
        await posteService.PostulerAsync(company.Id, posteId, candidateProfileId);

        using var scope = NewScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>()
            .SetActiveCompany(company.Id, company.SchemaName);

        var candidatureChecker = scope.ServiceProvider.GetRequiredService<ICandidatureExistenceChecker>();
        Assert.True(await candidatureChecker.ExisteCandidatureReelleAsync(candidateProfileId, company.Id));

        var grille = await scope.ServiceProvider.GetRequiredService<IEntretienService>()
            .GenererGrilleAsync(candidateProfileId, candidatUserId);

        Assert.NotNull(grille);
        Assert.NotEmpty(grille!.Groupes);
        Assert.Contains(grille.Groupes, g => g.QuestionsPourEntreprise.Count > 0);
    }

    /// <summary>
    /// Même flux URL : sans candidature réelle (ou CompanyId arbitraire), le garde-fou page refuse
    /// avant génération — réponse vide/nulle, jamais d'exception ni de fuite d'existence.
    /// </summary>
    [Fact]
    public async Task LeCandidatSansCandidatureVersCetteEntreprise_NObtientAucuneQuestion()
    {
        var suffix = Guid.NewGuid();
        var candidatUserId = $"entretien-candidat-sans-{suffix}";
        var employeUserId = $"entretien-candidat-sans-mgr-{suffix}";

        var company = await fixture.CreateCompanyAsync($"Entreprise Entretien Sans Postul {suffix}", employeUserId);

        using var setupScope = NewScope();
        var candidateProfileId = await setupScope.ServiceProvider.GetRequiredService<ICandidateProfileService>()
            .GetOrCreateProfileIdAsync(candidatUserId);

        // CompanyId connu mais aucune candidature — comme un candidat qui tente /candidat/entretien/{id}.
        using var scope = NewScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>()
            .SetActiveCompany(company.Id, company.SchemaName);

        var candidatureChecker = scope.ServiceProvider.GetRequiredService<ICandidatureExistenceChecker>();
        var entretienService = scope.ServiceProvider.GetRequiredService<IEntretienService>();

        Assert.False(await candidatureChecker.ExisteCandidatureReelleAsync(candidateProfileId, company.Id));
        // CompanyId arbitraire (inexistant) : même refus silencieux, sans exception.
        Assert.False(await candidatureChecker.ExisteCandidatureReelleAsync(candidateProfileId, companyId: int.MaxValue));

        // Miroir de EntretienCandidatPage : sans candidature réelle, on n'appelle pas GenererGrilleAsync
        // → réponse vide côté UI (message générique), jamais de fuite d'existence.
        GrilleEntretienView? grillePourPage = null;
        if (await candidatureChecker.ExisteCandidatureReelleAsync(candidateProfileId, company.Id))
            grillePourPage = await entretienService.GenererGrilleAsync(candidateProfileId, candidatUserId);

        Assert.Null(grillePourPage);

        GrilleEntretienView? grilleCompanyArbitraire = null;
        if (await candidatureChecker.ExisteCandidatureReelleAsync(candidateProfileId, int.MaxValue))
            grilleCompanyArbitraire = await entretienService.GenererGrilleAsync(candidateProfileId, candidatUserId);

        Assert.Null(grilleCompanyArbitraire);
    }
}
