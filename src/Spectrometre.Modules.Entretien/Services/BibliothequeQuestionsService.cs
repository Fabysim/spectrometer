using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Ai;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Entretien.Data;
using Spectrometre.Modules.Entretien.Entities;

namespace Spectrometre.Modules.Entretien.Services;

public sealed class BibliothequeQuestionsService(
    IDbContextFactory<EntretienCatalogDbContext> catalogFactory,
    IDbContextFactory<EntretienDbContext> entretienFactory,
    ITenantContext tenantContext,
    IReplicateService replicate) : IBibliothequeQuestionsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<IReadOnlyList<InterviewQuestionCategoryDto>> GetCatalogueAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await catalogFactory.CreateDbContextAsync(cancellationToken);
        var cats = await db.InterviewQuestionCategories.AsNoTracking()
            .Include(c => c.SubCategories)
            .ThenInclude(s => s.Questions)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);

        return cats.Select(c => new InterviewQuestionCategoryDto(
            c.Id,
            c.Name,
            c.SubCategories
                .OrderBy(s => s.SortOrder)
                .Select(s => new InterviewQuestionSubCategoryDto(
                    s.Id,
                    s.Name,
                    s.Questions
                        .OrderBy(q => q.SortOrder)
                        .Select(q => new InterviewQuestionItemDto(q.Id, q.Text, q.ExpectedElements))
                        .ToList()))
                .ToList())).ToList();
    }

    public async Task<IReadOnlyList<InterviewAnswerDto>> GetReponsesAsync(
        int candidateProfileId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await CreateTenantDbAsync(cancellationToken);
        var answers = await db.InterviewAnswers.AsNoTracking()
            .Where(a => a.CandidateProfileId == candidateProfileId)
            .ToListAsync(cancellationToken);

        return answers
            .Select(a => new InterviewAnswerDto(a.InterviewQuestionId, a.Response, a.CreatedAt, a.UpdatedAt))
            .ToList();
    }

    public async Task SaveReponsesAsync(
        int candidateProfileId,
        IReadOnlyList<InterviewAnswerInputDto> reponses,
        CancellationToken cancellationToken = default)
    {
        await using var db = await CreateTenantDbAsync(cancellationToken);
        var existing = await db.InterviewAnswers
            .Where(a => a.CandidateProfileId == candidateProfileId)
            .ToListAsync(cancellationToken);
        var byQuestion = existing.ToDictionary(a => a.InterviewQuestionId);
        var now = DateTimeOffset.UtcNow;

        foreach (var input in reponses)
        {
            if (byQuestion.TryGetValue(input.InterviewQuestionId, out var row))
            {
                row.Response = input.Response;
                row.UpdatedAt = now;
            }
            else if (!string.IsNullOrWhiteSpace(input.Response))
            {
                db.InterviewAnswers.Add(new InterviewAnswer
                {
                    InterviewQuestionId = input.InterviewQuestionId,
                    CandidateProfileId = candidateProfileId,
                    Response = input.Response,
                    CreatedAt = now,
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<(string? Transcript, string? Error)> TranscrireSegmentAsync(
        byte[] audioBytes,
        string mimeType,
        CancellationToken cancellationToken = default) =>
        replicate.TranscribeAudioAsync(audioBytes, mimeType, cancellationToken);

    public async Task<IReadOnlyDictionary<int, string>> ClassifierTranscriptionAsync(
        string transcript,
        IReadOnlyList<InterviewQuestionCategoryDto> catalogue,
        CancellationToken cancellationToken = default)
    {
        var questions = catalogue
            .SelectMany(c => c.SubCategories)
            .SelectMany(s => s.Questions)
            .ToList();
        if (questions.Count == 0 || string.IsNullOrWhiteSpace(transcript))
            return new Dictionary<int, string>();

        var validIds = questions.Select(q => q.Id).ToHashSet();
        var questionsList = string.Join(
            "\n",
            questions.Select(q => $"- [ID:{q.Id}] {q.Text}"));

        // Prompt système / utilisateur — port fidèle de ContextService.ClassifyInterviewTranscriptAsync (MVP).
        const string systemPrompt = """
            Tu es un assistant qui analyse la transcription brute d'une entrevue de recrutement (questions posées à voix haute par le recruteur, réponses du candidat).
            Tu reçois la liste des questions officielles de l'entrevue (avec leur ID) et la transcription complète de l'échange.
            Pour chaque question, retrouve dans la transcription la réponse donnée par le candidat et reproduis-la fidèlement (proche du verbatim, en nettoyant uniquement les hésitations orales évidentes comme "euh").
            Si une question n'a clairement pas été posée ou pas de réponse identifiable, ne l'inclus pas dans le résultat.
            Réponds UNIQUEMENT en JSON valide, sans texte avant ou après, au format exact :
            {
              "answers": [
                { "questionId": <ID numérique exact>, "response": "<réponse du candidat>" }
              ]
            }
            """;

        var userPrompt = $"""
            ## Questions de l'entrevue

            {questionsList}

            ## Transcription complète de l'entrevue

            {transcript}
            """;

        var (output, error) = await replicate.RunClaudeAsync(systemPrompt, userPrompt, cancellationToken);
        if (error is not null || string.IsNullOrWhiteSpace(output))
            return new Dictionary<int, string>();

        var json = StripMarkdownFences(output.Trim());
        try
        {
            var parsed = JsonSerializer.Deserialize<ClassifyTranscriptResponse>(json, JsonOptions);
            if (parsed?.Answers is null)
                return new Dictionary<int, string>();

            var result = new Dictionary<int, string>();
            foreach (var answer in parsed.Answers)
            {
                if (!validIds.Contains(answer.QuestionId))
                    continue;
                if (string.IsNullOrWhiteSpace(answer.Response))
                    continue;
                result[answer.QuestionId] = answer.Response.Trim();
            }

            return result;
        }
        catch (JsonException)
        {
            return new Dictionary<int, string>();
        }
    }

    private async Task<EntretienDbContext> CreateTenantDbAsync(CancellationToken cancellationToken)
    {
        var db = await entretienFactory.CreateDbContextAsync(cancellationToken);
        db.TenantSchema = tenantContext.SchemaName
            ?? throw new InvalidOperationException("Aucune entreprise active — réponses d'entrevue hors tenant.");
        return db;
    }

    private static string StripMarkdownFences(string text)
    {
        if (!text.StartsWith("```", StringComparison.Ordinal))
            return text;

        var lines = text.Split('\n');
        var start = 1;
        if (lines.Length > 0 && lines[0].TrimStart().StartsWith("```", StringComparison.Ordinal))
        {
            // skip ``` or ```json
        }

        var end = lines.Length;
        if (end > start && lines[^1].Trim().StartsWith("```", StringComparison.Ordinal))
            end--;

        return string.Join('\n', lines[start..end]).Trim();
    }

    private sealed record ClassifyTranscriptResponse(List<ClassifyTranscriptAnswer>? Answers);
    private sealed record ClassifyTranscriptAnswer(int QuestionId, string Response);
}
