namespace Spectrometre.Core.Billing;

/// <summary>
/// Association simple (plan → module inclus). <see cref="PlanCode"/> est une chaîne libre alignée sur
/// <see cref="Plan.Code"/> (référence molle, pas de FK) — les prix/périodicité vivent dans
/// <see cref="Plan"/>, pas ici.
/// </summary>
public sealed class PlanModuleEntitlement
{
    public int Id { get; set; }
    public required string PlanCode { get; set; }
    public required string ModuleCode { get; set; }
}
