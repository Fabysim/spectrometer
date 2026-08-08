namespace Spectrometre.Modules.ProfilEntreprise.Entities;

/// <summary>
/// Évaluation d'un critère pour une candidature : niveau déclaré par le candidat à la postulation,
/// puis niveau final ajusté par l'entreprise (nullable tant que non ajusté).
/// </summary>
public sealed class EvaluationCritereCandidature
{
    public int Id { get; set; }
    public int CandidatureId { get; set; }
    public int CritereId { get; set; }

    /// <summary>Auto-évaluation du candidat à la postulation — null si candidature créée hors grille (invitation / vivier).</summary>
    public NiveauEvaluation? NiveauDeclare { get; set; }

    /// <summary>Ajustement entreprise — null tant que non renseigné.</summary>
    public NiveauEvaluation? NiveauFinal { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
