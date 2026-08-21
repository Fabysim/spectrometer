namespace Spectrometre.Modules.JeunesPrestataires.Entities;

/// <summary>
/// Métadonnées du jeune portées par une invitation en attente — pré-remplissent
/// <see cref="JeuneProfile"/> à l'acceptation (<c>Invitation.Id</c> dans le schéma core).
/// </summary>
public sealed class InvitationJeunePrestataire
{
    public int Id { get; set; }

    /// <summary>Clé logique vers <c>core.Invitations.Id</c> — pas de FK cross-schéma EF.</summary>
    public int InvitationId { get; set; }

    public required string Nom { get; set; }
    public required string Prenoms { get; set; }
    public DateOnly DateNaissance { get; set; }

    /// <summary>Copié vers <see cref="JeuneProfile"/> à l'acceptation.</summary>
    public ProfilAccompagnement ProfilAccompagnement { get; set; } = ProfilAccompagnement.SansExperience;
}
