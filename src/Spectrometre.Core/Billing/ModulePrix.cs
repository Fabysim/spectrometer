namespace Spectrometre.Core.Billing;

/// <summary>
/// Tarif mensuel à la carte d'un module — source de vérité du montant dû
/// (<see cref="IFacturationCalculatorService"/>), indépendant du prix de <see cref="Plan"/>.
/// </summary>
public sealed class ModulePrix
{
    public int Id { get; set; }

    public required string ModuleCode { get; set; }

    public decimal PrixMensuel { get; set; }

    /// <summary>Devise libre (ex. <c>EUR</c>).</summary>
    public required string Devise { get; set; }

    /// <summary>
    /// <c>false</c> pour les modules socle toujours gratuits (profils de base, Admin) —
    /// exclus du calcul même s'ils sont actifs.
    /// </summary>
    public bool Facturable { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
