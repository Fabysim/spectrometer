namespace Spectrometre.Modules.GestionDuTemps.Entities;

public static class CycleStatuts
{
    public const string EnCours = "EnCours";
    public const string Termine = "Termine";

    // Pas de statut "Archive" : présent dans l'énum équivalente de mvp (GdtCycleStatuses) mais jamais
    // assigné nulle part dans son code — mort. Non repris ici (voir le résumé du cycle : fonctionnalité
    // évoquée mais jamais aboutie).
}

/// <summary>
/// Une "saison" de gestion du temps pour un utilisateur — repris de <c>GdtCycle</c> de <c>mvp</c>, sans
/// durée fixe (l'utilisateur clôture quand il veut, pas de date de fin planifiée). Un seul cycle
/// <see cref="CycleStatuts.EnCours"/> à la fois par utilisateur (contrainte d'unicité filtrée, même
/// principe que <c>IX_GdtCycles_UserId_EnCours</c> dans mvp).
/// </summary>
/// <remarks>
/// À la clôture (voir <c>GestionDuTempsService.ClotureEtDemarrerNouveauCycleAsync</c>) : les
/// <see cref="TypeDeTemps"/> du cycle clôturé sont COPIÉS vers le nouveau cycle (mêmes catégories
/// reconduites) ; les <see cref="Activite"/>, elles, ne sont PAS reportées — elles restent attachées au
/// cycle clôturé, consultables mais non actives (comportement identique à mvp, qui ne les reportait jamais
/// non plus). C'est un archivage passif, pas une suppression.
/// </remarks>
public sealed class Cycle
{
    public int Id { get; set; }

    public required string UserId { get; set; }

    public int NumeroCycle { get; set; } = 1;

    public string Statut { get; set; } = CycleStatuts.EnCours;

    public DateTimeOffset DemarreLe { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ClotureLe { get; set; }
}
