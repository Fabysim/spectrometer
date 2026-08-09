namespace Spectrometre.Modules.SuiviEmployes.Entities;

/// <summary>Clôture d'un bloc d'évaluation (équivalent mvp <c>ManagerSocioProEvaluationClosed</c>).</summary>
public sealed class EvaluationEmployeCloturee
{
    public int Id { get; set; }

    public int UserCompanyLinkId { get; set; }

    public int PosteId { get; set; }

    public DateOnly EvaluationDate { get; set; }
}
