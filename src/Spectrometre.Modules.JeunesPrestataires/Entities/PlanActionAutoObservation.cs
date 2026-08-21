namespace Spectrometre.Modules.JeunesPrestataires.Entities;

/// <summary>
/// Tableau 3 — plan d'action rédigé par le coach (document Bouchra). Un enregistrement
/// mutable par jeune, même schéma que <see cref="GuideEntrevue"/> / <see cref="ConsentementParental"/>
/// (upsert, pas d'historique). Visible en lecture par le jeune une fois rempli ; jamais éditable par lui.
/// La synthèse auto-générée n'est pas réécrite ici : « confirmer » = validation horodatée sur
/// <see cref="AutoObservationSyntheseGeneree"/> ; « compléter / nuancer » = ce plan (et le guide d'entrevue).
/// </summary>
public sealed class PlanActionAutoObservation
{
    public int Id { get; set; }
    public int JeuneProfileId { get; set; }

    public string? ObjectifPrincipal { get; set; }
    public string? PremiereAction { get; set; }
    public string? ResponsableSuivi { get; set; }
    public DateOnly? Echeance { get; set; }
    public string? IndicateurReussite { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
