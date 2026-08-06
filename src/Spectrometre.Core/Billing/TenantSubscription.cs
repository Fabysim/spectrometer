namespace Spectrometre.Core.Billing;

/// <summary>
/// Abonnement d'une entreprise — un par entreprise, créé automatiquement (plan
/// <see cref="PlanCodes.Standard"/>) dès sa création par <c>ICompanyProvisioningService.CreateCompanyAsync</c>.
/// Reste volontairement minimal (pas de logique de facturation réelle, Stripe ou autre viendrait plus
/// tard) mais sert désormais de base réelle au gating par plan : voir <see cref="PlanModuleEntitlement"/>
/// et <c>ModuleRegistry.IsActiveAsync</c> pour la règle exacte (actif seulement si à la fois activé ET
/// inclus dans le plan souscrit, avec un statut <see cref="SubscriptionStatus.Essai"/> ou
/// <see cref="SubscriptionStatus.Active"/>).
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
