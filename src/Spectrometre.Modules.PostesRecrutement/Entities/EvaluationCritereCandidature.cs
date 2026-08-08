namespace Spectrometre.Modules.PostesRecrutement.Entities;

/// <summary>
/// Niveau final ajusté par l'entreprise pour un critère donné sur une candidature
/// (équivalent des <c>FirstEvaluation</c> de type Final du MVP — sans niveau déclaré candidat).
/// </summary>
public sealed class EvaluationCritereCandidature
{
    public int Id { get; set; }
    public int CandidatureId { get; set; }
    public int CritereId { get; set; }
    public NiveauEvaluation NiveauFinal { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
