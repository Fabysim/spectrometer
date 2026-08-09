namespace Spectrometre.Modules.SuiviEmployes.Entities;

/// <summary>Cache d'analyse IA employé (équivalent mvp <c>ManagerAiAnalysis</c>).</summary>
public sealed class AnalyseIaEmploye
{
    public int Id { get; set; }

    public int UserCompanyLinkId { get; set; }

    public string DataHash { get; set; } = "";

    public string AnalyseMarkdown { get; set; } = "";

    public DateTimeOffset GenereeLe { get; set; } = DateTimeOffset.UtcNow;

    public bool EnCours { get; set; }

    /// <summary>True si le texte vient du fournisseur IA ; false pour le repli local.</summary>
    public bool GenereeParIa { get; set; }
}
