namespace Spectrometre.Modules.ProfilCoach.Entities;

/// <summary>
/// Profil d'un coach. Rattaché à un utilisateur (<c>core.AspNetUsers</c>), pas à une entreprise — même
/// raisonnement que <c>CandidateProfile</c> (ProfilCandidat) : un coach n'est pas un tenant, il peut suivre
/// des personnes rattachées à différentes entreprises ou candidats indépendants. Schéma fixe
/// (<c>profil_coach</c>), non tenant-scopé.
/// </summary>
public sealed class CoachProfile
{
    public int Id { get; set; }
    public required string UserId { get; set; }

    public string NomAffiche { get; set; } = "";
    public string BioCourte { get; set; } = "";

    /// <summary>Liste libre, séparée par virgules — même simplicité que les tags libres de Gestion du temps (pas de vocabulaire contrôlé pour ce premier cycle).</summary>
    public string Specialites { get; set; } = "";

    /// <summary>
    /// Opt-in explicite, désactivé par défaut — cohérent avec la logique de consentement déjà appliquée
    /// ailleurs dans le projet (Vivier : visibilité conditionnée à une candidature réelle ; CV :
    /// consentement formel côté candidat). Seul ce champ conditionne l'apparition dans l'annuaire des
    /// coachs (voir <c>ICoachProfileService.GetAnnuaireVisibleAsync</c>) — jamais déduit implicitement de
    /// l'activation du module.
    /// </summary>
    public bool VisibleDansAnnuaire { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
