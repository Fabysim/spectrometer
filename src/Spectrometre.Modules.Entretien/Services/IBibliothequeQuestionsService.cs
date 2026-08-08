using Spectrometre.Modules.Entretien.Entities;

namespace Spectrometre.Modules.Entretien.Services;

public sealed record InterviewQuestionItemDto(int Id, string Text, string? ExpectedElements);

public sealed record InterviewQuestionSubCategoryDto(
    int Id,
    string Name,
    IReadOnlyList<InterviewQuestionItemDto> Questions);

public sealed record InterviewQuestionCategoryDto(
    int Id,
    string Name,
    IReadOnlyList<InterviewQuestionSubCategoryDto> SubCategories);

public sealed record InterviewAnswerDto(
    int InterviewQuestionId,
    string? Response,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record InterviewAnswerInputDto(int InterviewQuestionId, string? Response);

/// <summary>
/// Bibliothèque statique de questions d'entrevue + transcription / classification (porté du MVP).
/// Séparé de <see cref="IEntretienService"/> (grille dynamique par axes de compatibilité).
/// </summary>
public interface IBibliothequeQuestionsService
{
    Task<IReadOnlyList<InterviewQuestionCategoryDto>> GetCatalogueAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InterviewAnswerDto>> GetReponsesAsync(
        int candidateProfileId,
        CancellationToken cancellationToken = default);

    Task SaveReponsesAsync(
        int candidateProfileId,
        IReadOnlyList<InterviewAnswerInputDto> reponses,
        CancellationToken cancellationToken = default);

    Task<(string? Transcript, string? Error)> TranscrireSegmentAsync(
        byte[] audioBytes,
        string mimeType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Classe une transcription vers les questions du catalogue (prompt MVP
    /// <c>ClassifyInterviewTranscriptAsync</c>). Retourne questionId → réponse.
    /// </summary>
    Task<IReadOnlyDictionary<int, string>> ClassifierTranscriptionAsync(
        string transcript,
        IReadOnlyList<InterviewQuestionCategoryDto> catalogue,
        CancellationToken cancellationToken = default);
}
