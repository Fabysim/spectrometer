using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Entretien.Services;
using Spectrometre.Modules.ProfilCandidat.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Bibliothèque de questions + classification IA (prompt MVP) via <see cref="FakeReplicateService"/>.
/// </summary>
[Collection("Base de données partagée")]
public sealed class BibliothequeQuestionsServiceTests(ServiceFixture fixture)
{
    private IServiceScope NewScope() => fixture.Services.CreateScope();

    [Fact]
    public async Task GetCatalogue_ContientLesTroisCategoriesMvp()
    {
        using var scope = NewScope();
        var svc = scope.ServiceProvider.GetRequiredService<IBibliothequeQuestionsService>();
        var catalogue = await svc.GetCatalogueAsync();

        Assert.Equal(3, catalogue.Count);
        Assert.Contains(catalogue, c => c.Name == "Motivation et projet professionnel");
        Assert.Contains(catalogue, c => c.Name == "Compétences et expérience");
        Assert.Contains(catalogue, c => c.Name == "Comportement et collaboration");

        var totalQuestions = catalogue.SelectMany(c => c.SubCategories).SelectMany(s => s.Questions).Count();
        Assert.Equal(31, totalQuestions);
    }

    [Fact]
    public async Task ClassifierTranscription_RelieLesReponsesAuxBonsQuestionId()
    {
        using var scope = NewScope();
        var svc = scope.ServiceProvider.GetRequiredService<IBibliothequeQuestionsService>();
        var catalogue = await svc.GetCatalogueAsync();
        var q1 = catalogue[0].SubCategories[0].Questions[0];
        var q2 = catalogue[0].SubCategories[0].Questions[1];

        var fake = (FakeReplicateService)scope.ServiceProvider
            .GetRequiredService<Spectrometre.Core.Ai.IReplicateService>();
        fake.Erreur = null;
        fake.Reponse = $$"""
            {
              "answers": [
                { "questionId": {{q1.Id}}, "response": "Réponse classée pour Q1" },
                { "questionId": {{q2.Id}}, "response": "Réponse classée pour Q2" },
                { "questionId": 999999, "response": "Id inconnu à ignorer" }
              ]
            }
            """;

        var classified = await svc.ClassifierTranscriptionAsync(
            "Transcription fictive de l'entrevue.",
            catalogue);

        Assert.Equal(2, classified.Count);
        Assert.Equal("Réponse classée pour Q1", classified[q1.Id]);
        Assert.Equal("Réponse classée pour Q2", classified[q2.Id]);
        Assert.False(classified.ContainsKey(999999));
    }

    [Fact]
    public async Task SaveEtGetReponses_PersisteParCandidatDansLeTenant()
    {
        var suffix = Guid.NewGuid();
        var company = await fixture.CreateCompanyAsync($"Entretien biblio {suffix}", $"owner-biblio-{suffix}");
        var candidatUserId = $"cand-biblio-{suffix}";

        int candidateProfileId;
        int questionId;
        using (var setup = NewScope())
        {
            candidateProfileId = await setup.ServiceProvider
                .GetRequiredService<ICandidateProfileService>()
                .GetOrCreateProfileIdAsync(candidatUserId);

            var catalogue = await setup.ServiceProvider
                .GetRequiredService<IBibliothequeQuestionsService>()
                .GetCatalogueAsync();
            questionId = catalogue[0].SubCategories[0].Questions[0].Id;
        }

        using var scope = NewScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>()
            .SetActiveCompany(company.Id, company.SchemaName);
        var svc = scope.ServiceProvider.GetRequiredService<IBibliothequeQuestionsService>();

        await svc.SaveReponsesAsync(candidateProfileId, [
            new InterviewAnswerInputDto(questionId, "Note manuelle de test")
        ]);

        var loaded = await svc.GetReponsesAsync(candidateProfileId);
        var row = Assert.Single(loaded.Where(a => a.InterviewQuestionId == questionId));
        Assert.Equal("Note manuelle de test", row.Response);
    }
}
