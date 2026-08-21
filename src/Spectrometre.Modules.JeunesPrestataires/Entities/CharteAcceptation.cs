namespace Spectrometre.Modules.JeunesPrestataires.Entities;

/// <summary>
/// Acceptation de la charte par le JEUNE uniquement.
/// La section 13 du document source prévoit aussi une signature parentale ; elle n'est PAS
/// recopiée ici — l'engagement parental est déjà capturé par <see cref="ConsentementParental"/>
/// (case <c>EngagementEncouragerCharte</c> + confirmation nominative). Pas de second circuit.
/// Confirmation = nom tapé (même mécanisme que le consentement parental), jamais une signature
/// électronique qualifiée.
/// </summary>
public sealed class CharteAcceptation
{
    public int Id { get; set; }
    public int JeuneProfileId { get; set; }

    /// <summary>Nom tapé par le jeune pour confirmer lecture et compréhension.</summary>
    public required string NomConfirmation { get; set; }

    public DateTimeOffset AccepteeLe { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
