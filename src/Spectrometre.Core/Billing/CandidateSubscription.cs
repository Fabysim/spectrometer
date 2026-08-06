namespace Spectrometre.Core.Billing;

/// <summary>
/// Équivalent de <see cref="TenantSubscription"/> côté candidat — même simplicité volontaire (statut, plan,
/// un point d'ancrage, pas de logique de facturation réelle). Un candidat n'a PAS d'abonnement par défaut
/// (contrairement à une entreprise, dont un abonnement <see cref="PlanCodes.Standard"/> est créé dès sa
/// création — voir <c>CompanyOnboardingService</c>) : sans abonnement, aucun module payant n'est actif pour
/// lui (échec fermé, voir <c>ModuleRegistry.IsActiveAsync</c>) — un candidat souscrit explicitement pour
/// débloquer un module comme Gestion du temps, ce n'est jamais groupé avec son profil de base.
/// </summary>
public sealed class CandidateSubscription
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }
    public required string PlanCode { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Essai;
    public DateTimeOffset? RenewalDate { get; set; }
}
