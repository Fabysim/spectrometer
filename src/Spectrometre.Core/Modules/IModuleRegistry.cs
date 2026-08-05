using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Data;

namespace Spectrometre.Core.Modules;

/// <summary>
/// Registre des modules disponibles (catalogue, renseigné une fois au démarrage par chaque
/// <c>AddXxxModule()</c>) et de leur activation par entreprise (table <see cref="ModuleActivation"/>).
/// </summary>
public interface IModuleRegistry
{
    /// <summary>Appelé par la méthode d'extension DI de chaque module, depuis <c>Program.cs</c>.</summary>
    void Register(ModuleManifest manifest);

    IReadOnlyList<ModuleManifest> AllModules { get; }

    ModuleManifest? Find(string moduleCode);

    /// <summary>Un module ne peut être activé que si tous les modules qu'il requiert le sont déjà.</summary>
    bool CanActivate(string moduleCode, IReadOnlyCollection<string> currentlyActiveCodes, out IReadOnlyList<string> missingDependencies);

    Task<IReadOnlyList<string>> GetActiveModuleCodesAsync(int companyId, CoreDbContext db, CancellationToken cancellationToken = default);

    Task<bool> IsActiveAsync(int companyId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default);

    /// <summary>Active un module pour une entreprise. Lève si une dépendance requise n'est pas déjà active.</summary>
    Task ActivateForCompanyAsync(int companyId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default);
}

public sealed class ModuleRegistry : IModuleRegistry
{
    private readonly List<ModuleManifest> _manifests = [];

    public void Register(ModuleManifest manifest)
    {
        if (_manifests.Any(m => m.Code == manifest.Code))
            return;
        _manifests.Add(manifest);
    }

    public IReadOnlyList<ModuleManifest> AllModules => _manifests;

    public ModuleManifest? Find(string moduleCode) => _manifests.FirstOrDefault(m => m.Code == moduleCode);

    public bool CanActivate(string moduleCode, IReadOnlyCollection<string> currentlyActiveCodes, out IReadOnlyList<string> missingDependencies)
    {
        var manifest = Find(moduleCode) ?? throw new InvalidOperationException($"Module inconnu : {moduleCode}");
        var missing = manifest.RequiredModuleCodes.Where(required => !currentlyActiveCodes.Contains(required)).ToList();
        missingDependencies = missing;
        return missing.Count == 0;
    }

    public async Task<IReadOnlyList<string>> GetActiveModuleCodesAsync(int companyId, CoreDbContext db, CancellationToken cancellationToken = default)
    {
        return await db.ModuleActivations
            .Where(a => a.CompanyId == companyId && a.IsActive)
            .Select(a => a.ModuleCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsActiveAsync(int companyId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default)
    {
        var active = await GetActiveModuleCodesAsync(companyId, db, cancellationToken);
        return active.Contains(moduleCode);
    }

    public async Task ActivateForCompanyAsync(int companyId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default)
    {
        var activeCodes = await GetActiveModuleCodesAsync(companyId, db, cancellationToken);
        if (!CanActivate(moduleCode, activeCodes, out var missing))
        {
            throw new InvalidOperationException(
                $"Impossible d'activer le module '{moduleCode}' : dépendance(s) manquante(s) : {string.Join(", ", missing)}.");
        }

        db.ModuleActivations.Add(new ModuleActivation { CompanyId = companyId, ModuleCode = moduleCode });
        await db.SaveChangesAsync(cancellationToken);
    }
}
