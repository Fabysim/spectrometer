namespace Spectrometre.Core.Billing;

/// <summary>
/// Équivalent de <see cref="CandidateSubscription"/> côté coach — même simplicité volontaire. Contrairement
/// au candidat (aucun abonnement par défaut), un abonnement <see cref="PlanCodes.Coach"/> est créé
/// automatiquement à l'inscription (voir <c>CoachOnboardingService</c>) : le module Profil Coach est
/// entièrement gratuit, il n'y a pas de raison de bloquer un coach fraîchement inscrit sans abonnement —
/// même logique que <see cref="TenantSubscription"/> pour une entreprise.
/// </summary>
public sealed class CoachSubscription
{
    public int Id { get; set; }
    public int CoachProfileId { get; set; }
    public required string PlanCode { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTimeOffset? RenewalDate { get; set; }
}
