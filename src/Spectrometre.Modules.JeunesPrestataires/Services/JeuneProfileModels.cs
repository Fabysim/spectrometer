namespace Spectrometre.Modules.JeunesPrestataires.Services;

public sealed record JeuneProfileView(
    int Id,
    string UserId,
    string Nom,
    string Prenoms,
    DateOnly DateNaissance,
    int InvitationId,
    DateTimeOffset CreatedAt);

public sealed record InvitationJeunePrestataireDraft(
    string Nom,
    string Prenoms,
    DateOnly DateNaissance);

public sealed record InviterJeuneResult(
    bool Success,
    string? ErrorKey,
    Spectrometre.Core.Invitations.Invitation? Invitation,
    string? LienAcceptation,
    bool EmailEnvoye);
