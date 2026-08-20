namespace Spectrometre.Core.JeunesPrestataires;

/// <summary>Invitation jeune prestataire émise par un coach, en attente d'acceptation par le jeune.</summary>
public sealed record JeunePrestataireInvitationPendingView(
    int InvitationId,
    string Email,
    string Nom,
    string Prenoms,
    DateTimeOffset EnvoyeeLe,
    DateTimeOffset ExpireLe,
    bool EstExpiree);
