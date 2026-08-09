using Spectrometre.Core.Modules;

namespace Spectrometre.Core.Billing;

/// <summary>
/// Enregistrement manuel d'un paiement reçu hors système (virement, chèque, etc.) —
/// aucun processeur de paiement intégré. Référence le sujet via
/// <see cref="SubjectType"/> + <see cref="SubjectId"/> (même principe que <c>ModuleActivation</c>).
/// </summary>
public sealed class PaiementEnregistre
{
    public int Id { get; set; }

    public ModuleActivationSubjectType SubjectType { get; set; }

    public int SubjectId { get; set; }

    public required string PlanCode { get; set; }

    public decimal Montant { get; set; }

    public required string Devise { get; set; }

    public DateOnly DateReception { get; set; }

    /// <summary>Moyen libre (ex. « Virement », « Chèque #1234 »).</summary>
    public required string Moyen { get; set; }

    public DateOnly PeriodeCouverteDebut { get; set; }

    public DateOnly PeriodeCouverteFin { get; set; }

    /// <summary>UserId ou email de l'admin qui a saisi l'enregistrement.</summary>
    public required string NotePar { get; set; }

    /// <summary>
    /// Snapshot des <c>ModuleCode</c> facturables au moment du paiement (séparés par virgule) —
    /// trace honnête si la liste d'activations change ensuite. <see cref="PlanCode"/> reste
    /// informatif seulement (plus la source du montant).
    /// </summary>
    public string? ModulesFactures { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
