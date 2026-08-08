namespace Spectrometre.Modules.Entretien.Entities;

/// <summary>
/// Réponse saisie (ou classifiée depuis une transcription) lors de l'entrevue entreprise.
/// Stockée dans le schéma tenant — unique par (InterviewQuestionId, CandidateProfileId).
/// </summary>
public sealed class InterviewAnswer
{
    public int Id { get; set; }

    /// <summary>Id de la question dans le catalogue public.</summary>
    public int InterviewQuestionId { get; set; }

    public int CandidateProfileId { get; set; }

    public string? Response { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
