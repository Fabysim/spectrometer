namespace Spectrometre.Modules.SuiviEmployes.Entities;

/// <summary>Objectif rattaché à une <see cref="EvaluationObjectifs"/> (équivalent mvp <c>Objective</c>).</summary>
public sealed class Objectif
{
    public int Id { get; set; }

    public int EvaluationObjectifsId { get; set; }

    public EvaluationObjectifs? EvaluationObjectifs { get; set; }

    public DateOnly Date { get; set; }

    public string Titre { get; set; } = "";

    public string? Moyens { get; set; }

    public AtteinteObjectif Atteinte { get; set; } = AtteinteObjectif.NonDefini;

    public string? Observation { get; set; }

    public int? Note { get; set; }
}
