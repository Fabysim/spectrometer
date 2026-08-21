namespace Spectrometre.Modules.JeunesPrestataires.Entities;

/// <summary>
/// Profil canonique d'un jeune prestataire — créé uniquement à l'acceptation d'une invitation coach
/// (<see cref="InvitationType.JeunePrestataire"/>), jamais via auto-inscription candidat.
/// </summary>
public sealed class JeuneProfile
{
    public int Id { get; set; }

    /// <summary>Compte Identity du jeune (<c>ApplicationUser.Id</c>).</summary>
    public required string UserId { get; set; }

    public required string Nom { get; set; }
    public required string Prenoms { get; set; }
    public DateOnly DateNaissance { get; set; }

    /// <summary>
    /// Décidé par le coach à l'invitation. Défaut <see cref="ProfilAccompagnement.SansExperience"/>
    /// (micro-tâches d'abord). Peut être corrigé ensuite sur la fiche de suivi.
    /// </summary>
    public ProfilAccompagnement ProfilAccompagnement { get; set; } = ProfilAccompagnement.SansExperience;

    /// <summary>Invitation ayant créé ce profil (traçabilité).</summary>
    public int InvitationId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
