namespace Spectrometre.Core.Invitations;

public enum InvitationStatus
{
    EnAttente = 0,
    Acceptee = 1,
    Expiree = 2,
    Revoquee = 3,
}

/// <summary>
/// Invitation générique par email — mécanisme volontairement neutre vis-à-vis de son usage final (voir
/// <see cref="InvitationType"/>), pensé dès le cycle Coaching pour resservir plus tard à d'autres parcours
/// (ex. inviter un manager sous une entreprise) sans refonte de schéma. Aucun envoi d'email réel dans ce
/// cycle (voir <c>IInvitationService</c>) : <see cref="Token"/> alimente un lien affiché à l'écran que
/// l'émetteur copie/partage lui-même — l'infrastructure d'envoi automatique est un chantier séparé.
/// </summary>
public sealed class Invitation
{
    public int Id { get; set; }
    public required string EmetteurUserId { get; set; }

    /// <summary>Toujours normalisé en minuscules à la création — comparé tel quel à l'email du compte qui accepte.</summary>
    public required string EmailInvite { get; set; }

    public InvitationType Type { get; set; }

    /// <summary>Réservé aux futurs types d'invitation ayant besoin d'un identifiant contextuel supplémentaire (ex. CompanyId pour un futur type manager). Inutilisé par <see cref="InvitationType.Coaching"/>.</summary>
    public int? ContextId { get; set; }

    /// <summary>Jeton aléatoire cryptographiquement sûr, encodé URL-safe — voir <c>InvitationService.GenererToken</c>.</summary>
    public required string Token { get; set; }

    public InvitationStatus Statut { get; set; } = InvitationStatus.EnAttente;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpireLe { get; set; }
    public DateTimeOffset? AccepteeLe { get; set; }
}
