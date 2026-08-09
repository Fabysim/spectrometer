namespace Spectrometre.Core.Billing;

/// <summary>
/// Abonnement d'une entreprise — un par entreprise, créé automatiquement (libellé
/// <see cref="PlanCodes.Standard"/>) dès sa création par <c>ICompanyProvisioningService.CreateCompanyAsync</c>.
/// Reste volontairement minimal (pas de logique de facturation réelle, Stripe ou autre viendrait plus
/// tard). <see cref="PlanCode"/> est une étiquette informative ; l'accès effectif aux modules dépend de
/// <c>ModuleActivation.IsActive</c> et du statut <see cref="SubscriptionStatus.Essai"/>/
/// <see cref="SubscriptionStatus.Active"/> (voir <c>ModuleRegistry.IsActiveAsync</c>).
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
