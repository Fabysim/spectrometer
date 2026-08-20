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
/// dépendances et le provisionnement de schéma.</description></item>
/// <item><description><c>IsActiveAsync</c> (et ses enveloppes) est la vérification "effective" — activé ET
/// abonnement du sujet en statut Essai/Active. C'est CETTE méthode que
/// <c>PosteService</c>/<c>CandidatureExistenceChecker</c>/<c>ProfileChangeRecorder</c> appellent déjà.
/// Un abonnement Suspendue/Résiliée (impayé) coupe donc l'accès sans toucher aux lignes d'activation.</description></item>
/// </list>
/// </remarks>
public interface IModuleRegistry
{
    /// <summary>Appelé par la méthode d'extension DI de chaque module, depuis <c>Program.cs</c>.</summary>
    void Register(ModuleManifest manifest);

    IReadOnlyList<ModuleManifest> AllModules { get; }

    ModuleManifest? Find(string moduleCode);

    /// <summary>Un module ne peut être activé que si tous les modules qu'il requiert le sont déjà (sur l'indicateur d'activation, pas l'abonnement — voir la remarque sur l'interface).</summary>
    bool CanActivate(string moduleCode, IReadOnlyCollection<string> currentlyActiveCodes, out IReadOnlyList<string> missingDependencies);

    // --- API généralisée par sujet ---

    Task<IReadOnlyList<string>> GetActiveModuleCodesAsync(ModuleActivationSubjectType subjectType, int subjectId, CoreDbContext db, CancellationToken cancellationToken = default);

    /// <summary>Vérification EFFECTIVE (activation + abonnement Essai/Active) — voir la remarque sur l'interface.</summary>
    Task<bool> IsActiveAsync(ModuleActivationSubjectType subjectType, int subjectId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default);

    /// <summary>Active un module pour un sujet. Lève si une dépendance requise n'est pas déjà active (indicateur).</summary>
    Task ActivateAsync(ModuleActivationSubjectType subjectType, int subjectId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default);

    /// <summary>
    /// Active OU désactive un module pour un sujet — idempotent et sûr à appeler plusieurs fois de suite
    /// (contrairement à <see cref="ActivateAsync"/> seul, qui lèverait une violation d'index unique si la
    /// ligne existe déjà). Introduit pour la zone Admin (voir <c>Spectrometre.Modules.Admin</c>), seul
    /// endroit d'écriture sur l'activation en dehors du parcours d'inscription/« Ajouter un module » —
    /// AUCUNE logique d'activation parallèle : délègue à <see cref="ActivateAsync"/> pour la création
    /// (même vérification de dépendances), se contente de basculer <see cref="ModuleActivation.IsActive"/>
    /// si la ligne existe déjà. Un module jamais activé qu'on tente de désactiver est un no-op silencieux.
    /// </summary>
    Task SetActiveAsync(ModuleActivationSubjectType subjectType, int subjectId, string moduleCode, bool isActive, CoreDbContext db, CancellationToken cancellationToken = default);

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

    // --- Équivalents côté coach ---

    Task<IReadOnlyList<string>> GetActiveModuleCodesForCoachAsync(int coachProfileId, CoreDbContext db, CancellationToken cancellationToken = default);

    Task<bool> IsActiveForCoachAsync(int coachProfileId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default);

    Task ActivateForCoachAsync(int coachProfileId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default);

    // --- Équivalents côté particulier ---

    Task<IReadOnlyList<string>> GetActiveModuleCodesForParticulierAsync(int particulierProfileId, CoreDbContext db, CancellationToken cancellationToken = default);

    Task<bool> IsActiveForParticulierAsync(int particulierProfileId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default);

    Task ActivateForParticulierAsync(int particulierProfileId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default);
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

        // Enforcement paiement : Suspendue/Résiliée (ou absence d'abonnement) coupe l'accès effectif.
        return await EstAbonnementEnCoursAsync(subjectType, subjectId, db, cancellationToken);
    }

    private static async Task<bool> EstAbonnementEnCoursAsync(ModuleActivationSubjectType subjectType, int subjectId, CoreDbContext db, CancellationToken cancellationToken)
    {
        switch (subjectType)
        {
            case ModuleActivationSubjectType.Company:
                return await db.TenantSubscriptions.AsNoTracking()
                    .AnyAsync(s => s.CompanyId == subjectId && (s.Status == SubscriptionStatus.Essai || s.Status == SubscriptionStatus.Active), cancellationToken);

            case ModuleActivationSubjectType.Candidate:
                return await db.CandidateSubscriptions.AsNoTracking()
                    .AnyAsync(s => s.CandidateProfileId == subjectId && (s.Status == SubscriptionStatus.Essai || s.Status == SubscriptionStatus.Active), cancellationToken);

            case ModuleActivationSubjectType.Coach:
                return await db.CoachSubscriptions.AsNoTracking()
                    .AnyAsync(s => s.CoachProfileId == subjectId && (s.Status == SubscriptionStatus.Essai || s.Status == SubscriptionStatus.Active), cancellationToken);

            case ModuleActivationSubjectType.Particulier:
                return await db.ParticulierSubscriptions.AsNoTracking()
                    .AnyAsync(s => s.ParticulierProfileId == subjectId && (s.Status == SubscriptionStatus.Essai || s.Status == SubscriptionStatus.Active), cancellationToken);

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

    public async Task SetActiveAsync(ModuleActivationSubjectType subjectType, int subjectId, string moduleCode, bool isActive, CoreDbContext db, CancellationToken cancellationToken = default)
    {
        var existing = await db.ModuleActivations.FirstOrDefaultAsync(
            a => a.SubjectType == subjectType && a.SubjectId == subjectId && a.ModuleCode == moduleCode, cancellationToken);

        if (existing is not null)
        {
            if (existing.IsActive == isActive)
                return;
            existing.IsActive = isActive;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (!isActive)
            return; // Rien à désactiver — jamais activé pour ce sujet.

        await ActivateAsync(subjectType, subjectId, moduleCode, db, cancellationToken);
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

    // --- Enveloppes "particulier" ---

    public Task<IReadOnlyList<string>> GetActiveModuleCodesForParticulierAsync(int particulierProfileId, CoreDbContext db, CancellationToken cancellationToken = default) =>
        GetActiveModuleCodesAsync(ModuleActivationSubjectType.Particulier, particulierProfileId, db, cancellationToken);

    public Task<bool> IsActiveForParticulierAsync(int particulierProfileId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default) =>
        IsActiveAsync(ModuleActivationSubjectType.Particulier, particulierProfileId, moduleCode, db, cancellationToken);

    public Task ActivateForParticulierAsync(int particulierProfileId, string moduleCode, CoreDbContext db, CancellationToken cancellationToken = default) =>
        ActivateAsync(ModuleActivationSubjectType.Particulier, particulierProfileId, moduleCode, db, cancellationToken);
}
