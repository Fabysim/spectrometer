namespace Spectrometre.Modules.ProfilEntreprise.Entities;

/// <summary>
/// Trace de génération IA des critères d'un poste — sert uniquement à l'idempotence via
/// <see cref="HashContexte"/> (titre / description / tâches / compétences). Les critères
/// générés vivent dans <see cref="CritereEvaluation"/> sans flag « généré par IA ».
/// </summary>
public sealed class GenerationCriteresIaPoste
{
    public int Id { get; set; }
    public int PosteId { get; set; }
    public string HashContexte { get; set; } = "";
    public DateTimeOffset GenereeLe { get; set; } = DateTimeOffset.UtcNow;
    public bool GenereeParIa { get; set; }
}
