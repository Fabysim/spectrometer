namespace Spectrometre.Modules.ProfilCandidat.Entities;

/// <summary>
/// Réponse d'un candidat à une question. <see cref="UpdatedAt"/> est la traçabilité temporelle
/// demandée en prévision du futur module « Suivi évolutif » (non implémenté ici).
/// </summary>
public sealed class CandidateAnswer
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }
    public int QuestionId { get; set; }
    public string? AnswerText { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
