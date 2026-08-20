namespace Spectrometre.Core.Billing;

/// <summary>
/// Abonnement du sujet Particulier — même simplicité que <see cref="CoachSubscription"/> (gratuit à l'inscription).
/// </summary>
public sealed class ParticulierSubscription
{
    public int Id { get; set; }
    public int ParticulierProfileId { get; set; }
    public required string PlanCode { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTimeOffset? RenewalDate { get; set; }
}
