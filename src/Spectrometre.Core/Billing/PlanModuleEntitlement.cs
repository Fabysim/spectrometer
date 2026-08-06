namespace Spectrometre.Core.Billing;

/// <summary>
/// Association simple (plan → module inclus) — donne du sens à <see cref="TenantSubscription"/>/
/// <see cref="CandidateSubscription"/>, jusque-là de purs points d'ancrage. Volontairement minimal : pas de
/// table <c>Plan</c> séparée, <see cref="PlanCode"/> est une chaîne libre comme <see cref="ModuleCode"/> —
/// voir <see cref="PlanCodes"/> pour les valeurs connues et les seeds dans <c>CoreDbContext</c>.
/// </summary>
public sealed class PlanModuleEntitlement
{
    public int Id { get; set; }
    public required string PlanCode { get; set; }
    public required string ModuleCode { get; set; }
}
