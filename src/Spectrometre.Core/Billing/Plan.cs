namespace Spectrometre.Core.Billing;

/// <summary>Périodicité d'affichage / facturation manuelle d'un <see cref="Plan"/>.</summary>
public enum PeriodicitePlan
{
    Mensuel = 0,
    Annuel = 1,
}

/// <summary>
/// Référentiel historique de prix par bundle <see cref="Code"/> — conservé pour l'admin/plans et
/// le gating (<c>*Subscription.PlanCode</c>), mais N'EST PLUS la source du montant facturé
/// (voir <see cref="ModulePrix"/> / <see cref="IFacturationCalculatorService"/>, tarification à la carte).
/// </summary>
public sealed class Plan
{
    public int Id { get; set; }

    /// <summary>Identifiant métier unique (ex. <see cref="PlanCodes.Standard"/>).</summary>
    public required string Code { get; set; }

    public required string Nom { get; set; }

    public decimal PrixMontant { get; set; }

    /// <summary>Devise libre (ex. <c>EUR</c>) — pas d'enum fermé.</summary>
    public required string PrixDevise { get; set; }

    public PeriodicitePlan Periodicite { get; set; } = PeriodicitePlan.Mensuel;

    /// <summary>Retire le plan de la vente sans casser les abonnements qui le référencent encore.</summary>
    public bool Actif { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
