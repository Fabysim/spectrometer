namespace Spectrometre.Modules.GestionDuTemps.Entities;

/// <summary>
/// Réflexion consciente d'un cycle — repris de <c>GdtReflexionConsciente</c> (mvp) : une seule entrée par
/// cycle (édition/mise à jour, pas un journal à entrées datées multiples — voir le résumé du cycle pour
/// cette lecture du code de mvp, où <c>GetReflexionAsync</c> retourne toujours une entité au plus, jamais
/// une liste). Un "moment d'arrêt" pour clarifier l'utilisation du temps, repris tel quel.
/// </summary>
public sealed class ReflexionConsciente
{
    public int Id { get; set; }
    public int CycleId { get; set; }
    public required string UserId { get; set; }

    public string? SituationActuelle { get; set; }
    public bool? SourceIdentifiee { get; set; }
    public DateOnly? DateEcheance { get; set; }

    public List<string> Ressentis { get; set; } = [];
    public List<string> Sources { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
