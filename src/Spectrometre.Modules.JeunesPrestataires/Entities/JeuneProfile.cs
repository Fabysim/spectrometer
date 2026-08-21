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
    /// Choisi par le coach à l'invitation, éventuellement remplacé une fois par la suggestion
    /// d'orientation (5 questions, écran d'auto-observation). Toute correction ultérieure du coach
    /// via la fiche de suivi reste prioritaire — l'orientation ne se rejoue pas.
    /// </summary>
    public ProfilAccompagnement ProfilAccompagnement { get; set; } = ProfilAccompagnement.SansExperience;

    /// <summary>Invitation ayant créé ce profil (traçabilité).</summary>
    public int InvitationId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
