namespace Spectrometre.Core.JeunesPrestataires;

/// <summary>Résultat d'un renvoi d'invitation jeune prestataire par le coach émetteur.</summary>
public sealed record RenvoyerJeunePrestataireInvitationResult(
    bool Success,
    bool EmailEnvoye,
    bool NouvelleInvitationCreee,
    int? InvitationId);
