using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.ProfilCandidat.Services;
using Spectrometre.Modules.ProfilEntreprise.Entities;
using Spectrometre.Modules.ProfilEntreprise.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Postulation avec grille d'auto-évaluation : refus si incomplète, création atomique
/// candidature + lignes <c>EvaluationCritereCandidature</c> avec <c>NiveauDeclare</c>.
/// </summary>
[Collection("Base de données partagée")]
public sealed class PostulerAvecGrilleTests(ServiceFixture fixture)
{
    private IServiceScope NewScope() => fixture.Services.CreateScope();

    [Fact]
    public async Task PostulerAvecGrille_RefuseSiGrilleIncomplete()
    {
        var suffix = Guid.NewGuid();
        var company = await fixture.CreateCompanyAsync($"Grille Incomplete {suffix}", $"owner-grille-inc-{suffix}");
        var candidatUserId = $"candidat-grille-inc-{suffix}";

        int posteId;
        int critereA;
        int critereB;
        int candidateProfileId;

        using (var scope = NewScope())
        {
            candidateProfileId = await scope.ServiceProvider
                .GetRequiredService<ICandidateProfileService>()
                .GetOrCreateProfileIdAsync(candidatUserId);

            scope.ServiceProvider.GetRequiredService<ITenantContext>()
                .SetActiveCompany(company.Id, company.SchemaName);
            var postes = scope.ServiceProvider.GetRequiredService<IPosteService>();

            posteId = await postes.CreatePosteAsync($"Poste grille {suffix}", "desc", "dept");
            await postes.UpsertCritereAsync(posteId, null, "Tech", "C#", (int)NiveauEvaluation.Fort, 1);
            await postes.UpsertCritereAsync(posteId, null, "Tech", "SQL", (int)NiveauEvaluation.Moyen, 2);
            var criteres = await postes.GetCriteresAsync(posteId);
            Assert.Equal(2, criteres.Count);
            critereA = criteres[0].Id;
            critereB = criteres[1].Id;
        }

        using (var scope = NewScope())
        {
            var postes = scope.ServiceProvider.GetRequiredService<IPosteService>();
            // Un seul critère déclaré sur deux
            var (succes, erreur) = await postes.PostulerAvecGrilleAsync(
                company.Id,
                posteId,
                candidateProfileId,
                new Dictionary<int, NiveauEvaluation> { [critereA] = NiveauEvaluation.Fort });

            Assert.False(succes);
            Assert.Contains("incomplète", erreur, StringComparison.OrdinalIgnoreCase);
        }

        using (var verify = NewScope())
        {
            verify.ServiceProvider.GetRequiredService<ITenantContext>()
                .SetActiveCompany(company.Id, company.SchemaName);
            var postes = verify.ServiceProvider.GetRequiredService<IPosteService>();
            Assert.Empty(await postes.GetCandidaturesAsync(posteId));
        }
    }

    [Fact]
    public async Task PostulerAvecGrille_CreeCandidatureEtNiveauxDeclaresAtomiquement()
    {
        var suffix = Guid.NewGuid();
        var company = await fixture.CreateCompanyAsync($"Grille Complete {suffix}", $"owner-grille-ok-{suffix}");
        var candidatUserId = $"candidat-grille-ok-{suffix}";

        int posteId;
        int candidateProfileId;
        Dictionary<int, NiveauEvaluation> declares;

        using (var scope = NewScope())
        {
            candidateProfileId = await scope.ServiceProvider
                .GetRequiredService<ICandidateProfileService>()
                .GetOrCreateProfileIdAsync(candidatUserId);

            scope.ServiceProvider.GetRequiredService<ITenantContext>()
                .SetActiveCompany(company.Id, company.SchemaName);
            var postes = scope.ServiceProvider.GetRequiredService<IPosteService>();

            posteId = await postes.CreatePosteAsync($"Poste grille ok {suffix}", "desc", "dept");
            await postes.UpsertCritereAsync(posteId, null, "Métier", "Analyse", (int)NiveauEvaluation.Fort, 1);
            await postes.UpsertCritereAsync(posteId, null, "Soft", "Communication", (int)NiveauEvaluation.Moyen, 2);
            await postes.UpsertCritereAsync(posteId, null, "Outils", "Excel", (int)NiveauEvaluation.Faible, 3);

            var criteres = await postes.GetCriteresAsync(posteId);
            Assert.Equal(3, criteres.Count);
            declares = new Dictionary<int, NiveauEvaluation>
            {
                [criteres[0].Id] = NiveauEvaluation.TresFort,
                [criteres[1].Id] = NiveauEvaluation.Moyen,
                [criteres[2].Id] = NiveauEvaluation.Faible,
            };
        }

        using (var scope = NewScope())
        {
            var postes = scope.ServiceProvider.GetRequiredService<IPosteService>();
            var (succes, erreur) = await postes.PostulerAvecGrilleAsync(
                company.Id, posteId, candidateProfileId, declares);
            Assert.True(succes, erreur);
            Assert.Null(erreur);
        }

        using (var verify = NewScope())
        {
            verify.ServiceProvider.GetRequiredService<ITenantContext>()
                .SetActiveCompany(company.Id, company.SchemaName);
            var postes = verify.ServiceProvider.GetRequiredService<IPosteService>();

            var candidature = Assert.Single(await postes.GetCandidaturesAsync(posteId));
            var evals = await postes.GetEvaluationCriteresAsync(candidature.Id);
            Assert.Equal(3, evals.Count);

            foreach (var eval in evals)
            {
                Assert.Equal(declares[eval.CritereId], eval.NiveauDeclare);
                Assert.Null(eval.NiveauFinal);
            }

            // Visible côté entreprise comme sur PosteCandidatureDetailPage
            Assert.All(evals, e => Assert.NotNull(e.NiveauDeclare));
        }
    }

    [Fact]
    public async Task PostulerAvecGrille_IdempotentSiDejaPostule()
    {
        var suffix = Guid.NewGuid();
        var company = await fixture.CreateCompanyAsync($"Grille Idem {suffix}", $"owner-grille-idem-{suffix}");
        var candidatUserId = $"candidat-grille-idem-{suffix}";

        int posteId;
        int candidateProfileId;
        Dictionary<int, NiveauEvaluation> declares;

        using (var scope = NewScope())
        {
            candidateProfileId = await scope.ServiceProvider
                .GetRequiredService<ICandidateProfileService>()
                .GetOrCreateProfileIdAsync(candidatUserId);

            scope.ServiceProvider.GetRequiredService<ITenantContext>()
                .SetActiveCompany(company.Id, company.SchemaName);
            var postes = scope.ServiceProvider.GetRequiredService<IPosteService>();
            posteId = await postes.CreatePosteAsync($"Poste idem {suffix}", null, null);
            await postes.UpsertCritereAsync(posteId, null, "A", "Crit", (int)NiveauEvaluation.Moyen, 1);
            var critereId = Assert.Single(await postes.GetCriteresAsync(posteId)).Id;
            declares = new Dictionary<int, NiveauEvaluation> { [critereId] = NiveauEvaluation.Fort };
        }

        using (var scope = NewScope())
        {
            var postes = scope.ServiceProvider.GetRequiredService<IPosteService>();
            Assert.True((await postes.PostulerAvecGrilleAsync(company.Id, posteId, candidateProfileId, declares)).Succes);
            Assert.True((await postes.PostulerAvecGrilleAsync(company.Id, posteId, candidateProfileId, declares)).Succes);
        }

        using (var verify = NewScope())
        {
            verify.ServiceProvider.GetRequiredService<ITenantContext>()
                .SetActiveCompany(company.Id, company.SchemaName);
            Assert.Single(await verify.ServiceProvider.GetRequiredService<IPosteService>().GetCandidaturesAsync(posteId));
        }
    }
}
