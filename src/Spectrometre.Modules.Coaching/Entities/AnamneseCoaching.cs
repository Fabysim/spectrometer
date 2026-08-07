namespace Spectrometre.Modules.Coaching.Entities;

/// <summary>
/// Anamnèse rédigée (avec aide de l'IA) par un coach à propos d'une personne suivie — un artefact PRIVÉ au
/// coach, distinct de la <c>Synthese</c> du module Gestion du temps (qui reste la synthèse propre de la
/// personne suivie, partagée en lecture avec le coach mais jamais éditée par lui). Une par
/// <see cref="LienCoaching"/> actif, régénérée à la demande — pas d'historique de versions dans ce cycle.
/// </summary>
public sealed class AnamneseCoaching
{
    public int Id { get; set; }
    public int LienCoachingId { get; set; }
    public required string Contenu { get; set; }
    public bool GenereeParIa { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
