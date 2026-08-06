namespace Spectrometre.Modules.GestionDuTemps.Entities;

/// <summary>
/// Synthèse d'un cycle — repris de <c>GdtSynthese</c> (mvp). <see cref="ProfilType"/>/<see cref="IndiceEquilibre"/>/
/// <see cref="NiveauMaturite"/> sont TOUJOURS calculés par un algorithme déterministe (voir
/// <c>SyntheseCalculator</c>), jamais hallucinés par l'IA. <see cref="ProfilTexte"/>/<see cref="IndiceCommentaire"/>/
/// <see cref="MaturiteCommentaire"/>/<see cref="RecommandationsJson"/>/<see cref="AlertesJson"/> viennent de l'IA
/// quand elle est disponible (<see cref="GenereeParIa"/> = true), sinon d'un texte de repli généré
/// localement (jamais d'erreur brute affichée à l'utilisateur — voir <c>GestionDuTempsService.GenererSyntheseAsync</c>).
/// </summary>
public sealed class Synthese
{
    public int Id { get; set; }
    public int CycleId { get; set; }
    public required string UserId { get; set; }

    public required string ProfilType { get; set; }
    public int IndiceEquilibre { get; set; }
    public int NiveauMaturite { get; set; }

    public string? ProfilTexte { get; set; }
    public string? IndiceCommentaire { get; set; }
    public string? MaturiteCommentaire { get; set; }

    /// <summary>JSON d'une liste de <c>RecommandationIa</c> (priorité, texte, domaine) — texte libre sérialisé, comme dans mvp, pas de table enfant pour ce cycle.</summary>
    public string? RecommandationsJson { get; set; }

    /// <summary>JSON d'une liste de chaînes.</summary>
    public string? AlertesJson { get; set; }

    public bool GenereeParIa { get; set; }

    /// <summary>Empreinte SHA-256 du profil (+ réflexion + résumé d'activité) utilisée pour éviter un nouvel appel IA si rien n'a changé.</summary>
    public string? ProfilSnapshotHash { get; set; }

    public DateTimeOffset CalculatedAt { get; set; } = DateTimeOffset.UtcNow;
}
