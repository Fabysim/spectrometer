namespace Spectrometre.Modules.SuiviEmployes.Entities;

/// <summary>
/// Score continu d'évaluation employé (équivalent mvp <c>ManagerEvaluationScore</c>).
/// <see cref="PosteId"/> / <see cref="CritereId"/> sont des clés logiques vers ProfilEntreprise
/// (lecture seule — pas de FK EF cross-DbContext, pas d'extension de <c>CritereEvaluation</c>).
/// </summary>
public sealed class EvaluationEmploye
{
    public int Id { get; set; }

    public int UserCompanyLinkId { get; set; }

    public int PosteId { get; set; }

    public int CritereId { get; set; }

    /// <summary>Score actuel 0–100.</summary>
    public int ScoreActuel { get; set; }

    /// <summary>Score souhaité 0–100.</summary>
    public int ScoreSouhaite { get; set; }

    public DateOnly EvaluationDate { get; set; }

    /// <summary>Séquence du jour (plusieurs saisies le même jour).</summary>
    public int DaySequence { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsClosed { get; set; }
}
