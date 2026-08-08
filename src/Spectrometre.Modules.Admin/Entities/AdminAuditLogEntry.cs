namespace Spectrometre.Modules.Admin.Entities;

/// <summary>
/// Trace minimale des actions d'écriture de la zone Admin (qui, quoi, sur quel sujet/plan, quand) —
/// introduite avec les premières actions d'écriture réelles au-delà de la promotion/rétrogradation
/// (activer/désactiver un module pour un client, éditer un plan). Pas un système d'audit élaboré :
/// une table consultable depuis <c>/admin</c> suffit pour ce cycle, voir la demande d'origine.
/// </summary>
public sealed class AdminAuditLogEntry
{
    public int Id { get; set; }
    public required string AdminUserId { get; set; }
    public required string Action { get; set; }

    /// <summary>Texte libre décrivant la cible — ex. "Company #12 / GestionDuTemps" ou "Plan Standard / Analytics" — jamais de contenu métier saisi par un utilisateur, uniquement des identifiants/codes structurels.</summary>
    public required string Cible { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
