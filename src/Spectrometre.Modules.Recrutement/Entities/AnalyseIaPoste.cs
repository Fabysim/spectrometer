namespace Spectrometre.Modules.Recrutement.Entities;

/// <summary>
/// Analyse IA de compatibilité poste/candidature (équivalent allégé de <c>PositionAiAnalysis</c> du MVP,
/// scopée à une candidature plutôt qu'à tout le poste). Cache invalidé via <see cref="HashSnapshot"/>.
/// </summary>
public sealed class AnalyseIaPoste
{
    public int Id { get; set; }
    public int PosteId { get; set; }
    public int CandidatureId { get; set; }
    public string AnalyseTexte { get; set; } = "";
    public DateTimeOffset GenereeLe { get; set; } = DateTimeOffset.UtcNow;
    public bool GenereeParIa { get; set; }
    public string? HashSnapshot { get; set; }
}
