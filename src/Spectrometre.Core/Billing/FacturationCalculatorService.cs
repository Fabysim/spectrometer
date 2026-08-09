using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Data;
using Spectrometre.Core.Modules;

namespace Spectrometre.Core.Billing;

public sealed record ModuleLigneFacture(string ModuleCode, decimal PrixMensuel);

/// <summary>Résultat de <see cref="IFacturationCalculatorService.CalculerMontantDuAsync"/>.</summary>
public sealed record MontantDuResult(
    decimal Total,
    string Devise,
    IReadOnlyList<ModuleLigneFacture> Lignes);

/// <summary>
/// Calcule le montant dû à la carte : somme des <see cref="ModulePrix.PrixMensuel"/> des modules
/// effectivement actifs (<see cref="IModuleRegistry.IsActiveAsync"/>) et <see cref="ModulePrix.Facturable"/>.
/// Lecture seule — n'écrit jamais sur <c>ModuleActivation</c>.
/// </summary>
public interface IFacturationCalculatorService
{
    Task<MontantDuResult> CalculerMontantDuAsync(
        ModuleActivationSubjectType subjectType,
        int subjectId,
        CoreDbContext db,
        CancellationToken cancellationToken = default);
}

public sealed class FacturationCalculatorService(IModuleRegistry moduleRegistry) : IFacturationCalculatorService
{
    public async Task<MontantDuResult> CalculerMontantDuAsync(
        ModuleActivationSubjectType subjectType,
        int subjectId,
        CoreDbContext db,
        CancellationToken cancellationToken = default)
    {
        var tarifs = await db.ModulePrix.AsNoTracking()
            .ToDictionaryAsync(t => t.ModuleCode, cancellationToken);

        // Codes cochés en base — on filtre ensuite via IsActiveAsync (activation + plan + statut).
        var coches = await moduleRegistry.GetActiveModuleCodesAsync(subjectType, subjectId, db, cancellationToken);
        var lignes = new List<ModuleLigneFacture>();
        var devise = "EUR";

        foreach (var code in coches.OrderBy(c => c))
        {
            if (!await moduleRegistry.IsActiveAsync(subjectType, subjectId, code, db, cancellationToken))
                continue;

            if (!tarifs.TryGetValue(code, out var tarif) || !tarif.Facturable)
                continue;

            devise = tarif.Devise;
            lignes.Add(new ModuleLigneFacture(code, tarif.PrixMensuel));
        }

        return new MontantDuResult(lignes.Sum(l => l.PrixMensuel), devise, lignes);
    }
}
