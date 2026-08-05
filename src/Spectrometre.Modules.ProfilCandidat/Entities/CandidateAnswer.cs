namespace Spectrometre.Modules.ProfilCandidat.Entities;

/// <summary>
/// Réponse d'un candidat à une question. Chaque changement est tracé par le module Suivi Évolutif
/// (via <c>IProfileChangeRecorder</c>, appelé depuis <c>CandidateProfileService.SaveAnswerAsync</c>) —
/// <see cref="UpdatedAt"/> reste la traçabilité locale minimale (horodatage de la dernière modification).
/// </summary>
public sealed class CandidateAnswer
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }
    public int QuestionId { get; set; }
    public string? AnswerText { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
