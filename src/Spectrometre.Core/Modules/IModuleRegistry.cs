using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;

namespace Spectrometre.Core.Modules;

/// <summary>
/// Registre des modules disponibles (catalogue, renseigné une fois au démarrage par chaque
/// <c>AddXxxModule()</c>) et de leur activation par SUJET (voir <see cref="ModuleActivationSubjectType"/> —
/// généralisé depuis le couplage exclusif à l'entreprise d'origine, table <see cref="ModuleActivation"/>).
/// </summary>
/// <remarks>
/// Distinction volontaire entre deux notions, pour ne rien casser du provisionnement de schéma existant :
/// <list type="bullet">
/// <item><description><c>GetActiveModuleCodesAsync</c>/<c>CanActivate</c>/<c>Activate*Async</c> raisonnent
/// sur le seul indicateur d'activation (<see cref="ModuleActivation.IsActive"/>) — "qu'est-ce qui a été
/// activé", utilisé par <c>TenantSchemaSynchronizer</c>/<c>CompanyOnboardingService</c> pour la chaîne de
/// dépendances et le provisionnement de schéma. Comportement INCHANGÉ par le gating par plan.</description></item>
/// <item><description><c>IsActiveAsync</c> (et ses enveloppes) est la vérification "effective" — activé ET
/// inclus dans le plan souscrit (voir <see cref="PlanModuleEntitlement"/>) — c'est CETTE méthode que
/// <c>PosteService</c>/<c>CandidatureExistenceChecker</c>/<c>ProfileChangeRecorder</c> appellent déjà, donc
/// le gating par plan s'y applique automatiquement, sans toucher leur code.</description></item>
/// </list>
/// Règle exacte du gating : un module est effectivement actif pour un sujet SEULEMENT SI (1) une ligne
/// <see cref="ModuleActivation"/> active existe ET (2) le sujet a un abonnement de statut Essai ou Active
/// ET (3) le plan de cet abonnement inclut ce module (<see cref="PlanModuleEntitlement"/>). Un module activé
/// mais absent du plan reste explicitement activé en base (l'indicateur n'est jamais modifié) — il
/// redevient effectif dès que le plan change, sans ré-activation manuelle.
/// </remarks>
public interface IModuleRegistry
{
    /// <summary>Appelé par la méthode d'extension DI de chaque module, depuis <c>Program.cs</c>.</summary>
    void Register(ModuleManifest manifest);

    IReadOnlyList<ModuleManifest> AllModules { get; }

    ModuleManifest? Find(string moduleCode);

    /// <summary>Un module ne peut être activé que si tous les modules qu'il requiert le sont déjà (sur l'indicateur d'activation, pas le plan — voir la remarque sur l'interface).</summary>
    bool CanActivate(string moduleCode, IReadOnlyCollection<string> currentlyActiveCodes, out IReadOnlyList<string> missingDependencies);

    // --- API généralisée par sujet ---

    Task<IReadOnlyList<string>> GetActiveModuleCodesAsync(ModuleActivationSubjectType subjectType, int subjectId, CoreDbContext db, CancellationToken cancellationToken = default);

    /// <summary>Vérification EFFECTIVE (activation + plan) — voir la remarque sur l'interface pour la règle exacte.</summary>
    Task<bool> IsActiveAsync(ModuleActivationSubjectType subjectType, int subjectId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default);

    /// <summary>Active un module pour un sujet. Lève si une dépendance requise n'est pas déjà active (indicateur, pas plan).</summary>
    Task ActivateAsync(ModuleActivationSubjectType subjectType, int subjectId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default);

    // --- Enveloppes fines "entreprise", pour compatibilité ascendante stricte : aucun appelant existant
    //     (PosteService, CandidatureExistenceChecker, ProfileChangeRecorder, CompanyOnboardingService,
    //     TenantSchemaSynchronizer, ServiceFixture) n'a besoin d'être modifié. ---

    Task<IReadOnlyList<string>> GetActiveModuleCodesAsync(int companyId, CoreDbContext db, CancellationToken cancellationToken = default);

    Task<bool> IsActiveAsync(int companyId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default);

    Task ActivateForCompanyAsync(int companyId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default);

    // --- Équivalents côté candidat ---

    Task<IReadOnlyList<string>> GetActiveModuleCodesForCandidateAsync(int candidateProfileId, CoreDbContext db, CancellationToken cancellationToken = default);

    Task<bool> IsActiveForCandidateAsync(int candidateProfileId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default);

    Task ActivateForCandidateAsync(int candidateProfileId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default);

    // --- Équivalents côté coach (nouveaux ce cycle) ---

    Task<IReadOnlyList<string>> GetActiveModuleCodesForCoachAsync(int coachProfileId, CoreDbContext db, CancellationToken cancellationToken = default);

    Task<bool> IsActiveForCoachAsync(int coachProfileId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default);

    Task ActivateForCoachAsync(int coachProfileId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default);
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

    public async Task<IReadOnlyList<string>> GetActiveModuleCodesAsync(ModuleActivationSubjectType subjectType, int subjectId, CoreDbContext db, CancellationToken cancellationToken = default)
    {
        return await db.ModuleActivations
            .Where(a => a.SubjectType == subjectType && a.SubjectId == subjectId && a.IsActive)
            .Select(a => a.ModuleCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsActiveAsync(ModuleActivationSubjectType subjectType, int subjectId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default)
    {
        var active = await GetActiveModuleCodesAsync(subjectType, subjectId, db, cancellationToken);
        if (!active.Contains(moduleCode))
            return false;

        var planCode = await GetPlanCodeAsync(subjectType, subjectId, db, cancellationToken);
        if (planCode is null)
            return false; // Échec fermé : aucun abonnement (ou statut Suspendue/Résiliée) => aucun module payant actif.

        return await db.PlanModuleEntitlements
            .AsNoTracking()
            .AnyAsync(e => e.PlanCode == planCode && e.ModuleCode == moduleCode, cancellationToken);
    }

    private static async Task<string?> GetPlanCodeAsync(ModuleActivationSubjectType subjectType, int subjectId, CoreDbContext db, CancellationToken cancellationToken)
    {
        // Seuls Essai et Active valent une entente en cours — Suspendue/Résiliée équivaut à pas d'abonnement.
        switch (subjectType)
        {
            case ModuleActivationSubjectType.Company:
                var tenantSub = await db.TenantSubscriptions.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.CompanyId == subjectId, cancellationToken);
                return tenantSub is { Status: SubscriptionStatus.Essai or SubscriptionStatus.Active } ? tenantSub.PlanCode : null;

            case ModuleActivationSubjectType.Candidate:
                var candidateSub = await db.CandidateSubscriptions.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.CandidateProfileId == subjectId, cancellationToken);
                return candidateSub is { Status: SubscriptionStatus.Essai or SubscriptionStatus.Active } ? candidateSub.PlanCode : null;

            case ModuleActivationSubjectType.Coach:
                var coachSub = await db.CoachSubscriptions.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.CoachProfileId == subjectId, cancellationToken);
                return coachSub is { Status: SubscriptionStatus.Essai or SubscriptionStatus.Active } ? coachSub.PlanCode : null;

            default:
                throw new NotSupportedException($"Type de sujet d'activation non pris en charge : {subjectType}");
        }
    }

    public async Task ActivateAsync(ModuleActivationSubjectType subjectType, int subjectId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default)
    {
        var activeCodes = await GetActiveModuleCodesAsync(subjectType, subjectId, db, cancellationToken);
        if (!CanActivate(moduleCode, activeCodes, out var missing))
        {
            throw new InvalidOperationException(
                $"Impossible d'activer le module '{moduleCode}' : dépendance(s) manquante(s) : {string.Join(", ", missing)}.");
        }

        db.ModuleActivations.Add(new ModuleActivation { SubjectType = subjectType, SubjectId = subjectId, ModuleCode = moduleCode });
        await db.SaveChangesAsync(cancellationToken);
    }

    // --- Enveloppes "entreprise" ---

    public Task<IReadOnlyList<string>> GetActiveModuleCodesAsync(int companyId, CoreDbContext db, CancellationToken cancellationToken = default) =>
        GetActiveModuleCodesAsync(ModuleActivationSubjectType.Company, companyId, db, cancellationToken);

    public Task<bool> IsActiveAsync(int companyId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default) =>
        IsActiveAsync(ModuleActivationSubjectType.Company, companyId, moduleCode, db, cancellationToken);

    public Task ActivateForCompanyAsync(int companyId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default) =>
        ActivateAsync(ModuleActivationSubjectType.Company, companyId, moduleCode, db, cancellationToken);

    // --- Enveloppes "candidat" ---

    public Task<IReadOnlyList<string>> GetActiveModuleCodesForCandidateAsync(int candidateProfileId, CoreDbContext db, CancellationToken cancellationToken = default) =>
        GetActiveModuleCodesAsync(ModuleActivationSubjectType.Candidate, candidateProfileId, db, cancellationToken);

    public Task<bool> IsActiveForCandidateAsync(int candidateProfileId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default) =>
        IsActiveAsync(ModuleActivationSubjectType.Candidate, candidateProfileId, moduleCode, db, cancellationToken);

    public Task ActivateForCandidateAsync(int candidateProfileId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default) =>
        ActivateAsync(ModuleActivationSubjectType.Candidate, candidateProfileId, moduleCode, db, cancellationToken);

    // --- Enveloppes "coach" ---

    public Task<IReadOnlyList<string>> GetActiveModuleCodesForCoachAsync(int coachProfileId, CoreDbContext db, CancellationToken cancellationToken = default) =>
        GetActiveModuleCodesAsync(ModuleActivationSubjectType.Coach, coachProfileId, db, cancellationToken);

    public Task<bool> IsActiveForCoachAsync(int coachProfileId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default) =>
        IsActiveAsync(ModuleActivationSubjectType.Coach, coachProfileId, moduleCode, db, cancellationToken);

    public Task ActivateForCoachAsync(int coachProfileId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default) =>
        ActivateAsync(ModuleActivationSubjectType.Coach, coachProfileId, moduleCode, db, cancellationToken);
}
