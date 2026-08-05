namespace Spectrometre.Core.Billing;

/// <summary>
/// Abonnement d'une entreprise. Volontairement minimal pour ce cycle — sert de point d'ancrage
/// pour la facturation réelle (Stripe ou autre) à brancher plus tard, sans toucher au noyau.
/// </summary>
public sealed class TenantSubscription
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public required string PlanCode { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Essai;
    public DateTimeOffset? RenewalDate { get; set; }
}

public enum SubscriptionStatus
{
    Essai = 0,
    Active = 1,
    Suspendue = 2,
    Resiliee = 3,
}
