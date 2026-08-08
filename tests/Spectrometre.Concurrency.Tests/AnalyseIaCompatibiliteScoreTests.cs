using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Data;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.ProfilCandidat.Services;
using Spectrometre.Modules.ProfilEntreprise.Services;
using Spectrometre.Modules.Recrutement.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Vérifie que <see cref="IRecrutementEntretienService.GenererAnalyseIaAsync"/> injecte le score
/// de compatibilité tags dans le prompt IA lorsque Compatibilite est actif, et le laisse absent
/// (n/d) lorsque le module est inactif — correction du bug <c>score = null</c> hardcodé.
/// </summary>
[Collection("Base de données partagée")]
public sealed class AnalyseIaCompatibiliteScoreTests(ServiceFixture fixture)
{
    private IServiceScope NewScope() => fixture.Services.CreateScope();

    [Fact]
    public async Task GenererAnalyseIa_AvecCompatibiliteActif_TransmetScoreTagsAuPrompt()
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
        try
        {
            var suffix = Guid.NewGuid();
            var ownerUserId = $"analyse-score-owner-{suffix}";
            var candidatUserId = $"analyse-score-candidat-{suffix}";
            var company = await fixture.CreateCompanyAsync($"Entreprise Analyse Score {suffix}", ownerUserId);

            int candidatureId;
            using (var setupScope = NewScope())
            {
                var candidateProfileId = await setupScope.ServiceProvider
                    .GetRequiredService<ICandidateProfileService>()
                    .GetOrCreateProfileIdAsync(candidatUserId);

                setupScope.ServiceProvider.GetRequiredService<ITenantContext>()
                    .SetActiveCompany(company.Id, company.SchemaName);
                var posteService = setupScope.ServiceProvider.GetRequiredService<IPosteService>();
                var posteId = await posteService.CreatePosteAsync($"Poste score {suffix}", "Desc", null);
                await posteService.PostulerAsync(company.Id, posteId, candidateProfileId);
                candidatureId = Assert.Single(await posteService.GetCandidaturesAsync(posteId)).Id;
            }

            using var scope = NewScope();
            scope.ServiceProvider.GetRequiredService<ITenantContext>()
                .SetActiveCompany(company.Id, company.SchemaName);

            var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
            Assert.True(await moduleRegistry.IsActiveAsync(company.Id, "Compatibilite", coreDb));

            var fakeIa = scope.ServiceProvider.GetRequiredService<IAnalysePosteIaService>() as FakeAnalysePosteIaService
                ?? throw new InvalidOperationException("FakeAnalysePosteIaService attendu.");
            fakeIa.ResetCaptures();
            fakeIa.Erreur = null;
            fakeIa.Reponse = "Analyse de test avec score.";

            var entretien = scope.ServiceProvider.GetRequiredService<IRecrutementEntretienService>();
            var analyse = await entretien.GenererAnalyseIaAsync(candidatureId, forcerRegeneration: true);

            Assert.True(analyse.GenereeParIa);
            Assert.Equal(1, fakeIa.CallCount);
            Assert.False(string.IsNullOrWhiteSpace(fakeIa.LastUserPrompt));
            Assert.False(string.IsNullOrWhiteSpace(fakeIa.LastSystemPrompt));

            Assert.Contains("Score de compatibilité (tags)", fakeIa.LastUserPrompt!, StringComparison.Ordinal);
            Assert.DoesNotContain("Score de compatibilité (tags) : n/d", fakeIa.LastUserPrompt!, StringComparison.Ordinal);
            Assert.Matches(@"Score de compatibilité \(tags\) : \d+%", fakeIa.LastUserPrompt!);
            Assert.Contains("Scores par axe", fakeIa.LastUserPrompt!, StringComparison.Ordinal);

            Assert.Contains("score de compatibilité par tags", fakeIa.LastSystemPrompt!, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("divergent", fakeIa.LastSystemPrompt!, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public async Task GenererAnalyseIa_SansCompatibilite_LaisseScoreAbsentDuPrompt()
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
        try
        {
            var suffix = Guid.NewGuid();
            var ownerUserId = $"analyse-noscore-owner-{suffix}";
            var candidatUserId = $"analyse-noscore-candidat-{suffix}";
            var company = await fixture.CreateCompanyAsync($"Entreprise Analyse Sans Score {suffix}", ownerUserId);

            using (var deactivateScope = NewScope())
            {
                var coreDb = deactivateScope.ServiceProvider.GetRequiredService<CoreDbContext>();
                var moduleRegistry = deactivateScope.ServiceProvider.GetRequiredService<IModuleRegistry>();
                await moduleRegistry.SetActiveAsync(
                    ModuleActivationSubjectType.Company, company.Id, "Compatibilite", isActive: false, coreDb);
                Assert.False(await moduleRegistry.IsActiveAsync(company.Id, "Compatibilite", coreDb));
            }

            int candidatureId;
            using (var setupScope = NewScope())
            {
                var candidateProfileId = await setupScope.ServiceProvider
                    .GetRequiredService<ICandidateProfileService>()
                    .GetOrCreateProfileIdAsync(candidatUserId);

                setupScope.ServiceProvider.GetRequiredService<ITenantContext>()
                    .SetActiveCompany(company.Id, company.SchemaName);
                var posteService = setupScope.ServiceProvider.GetRequiredService<IPosteService>();
                var posteId = await posteService.CreatePosteAsync($"Poste sans score {suffix}", "Desc", null);
                await posteService.PostulerAsync(company.Id, posteId, candidateProfileId);
                candidatureId = Assert.Single(await posteService.GetCandidaturesAsync(posteId)).Id;
            }

            using var scope = NewScope();
            scope.ServiceProvider.GetRequiredService<ITenantContext>()
                .SetActiveCompany(company.Id, company.SchemaName);

            var fakeIa = scope.ServiceProvider.GetRequiredService<IAnalysePosteIaService>() as FakeAnalysePosteIaService
                ?? throw new InvalidOperationException("FakeAnalysePosteIaService attendu.");
            fakeIa.ResetCaptures();
            fakeIa.Erreur = null;
            fakeIa.Reponse = "Analyse de test sans score.";

            var entretien = scope.ServiceProvider.GetRequiredService<IRecrutementEntretienService>();
            _ = await entretien.GenererAnalyseIaAsync(candidatureId, forcerRegeneration: true);

            Assert.Equal(1, fakeIa.CallCount);
            Assert.Contains("Score de compatibilité : n/d", fakeIa.LastUserPrompt!, StringComparison.Ordinal);
            Assert.DoesNotContain("Score de compatibilité (tags)", fakeIa.LastUserPrompt!, StringComparison.Ordinal);
            Assert.DoesNotContain("Scores par axe", fakeIa.LastUserPrompt!, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
