using System.Security.Claims;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Modules;

namespace Spectrometre.Modules.Admin.Services;

// Toutes les vues ci-dessous sont volontairement limitées à la métadonnée structurelle (qui, quel statut,
// quels modules, quelles dates) — jamais le contenu métier saisi par un utilisateur (réponses de
// questionnaire, CV, profil psychosocial, notes de coaching...). Décision délibérée pour ce premier cycle,
// pas un oubli à combler plus tard sans y repenser — voir la demande d'origine.

public sealed record AdminCompanySummary(int Id, string Name, string? OwnerEmail, string StatutAbonnement, string PlanCode, IReadOnlyList<string> ModulesActifs, DateTimeOffset CreatedAt);

public sealed record AdminCandidateSummary(int CandidateProfileId, string UserId, string? Email, string StatutAbonnement, string? PlanCode, IReadOnlyList<string> ModulesActifs, DateTimeOffset CreatedAt);

public sealed record AdminCoachSummary(int CoachProfileId, string UserId, string? Email, string NomAffiche, bool VisibleDansAnnuaire, int NombrePersonnesSuivies, IReadOnlyList<string> ModulesActifs, DateTimeOffset CreatedAt);

public sealed record AdminCoachingLinkSummary(int Id, string SuiviEmail, string CoachEmail, string Statut, DateTimeOffset CreatedAt, DateTimeOffset? AccepteLe);

public sealed record AdminInvitationSummary(int Id, string EmetteurEmail, string EmailInvite, string Type, string Statut, DateTimeOffset CreatedAt, DateTimeOffset ExpireLe, DateTimeOffset? AccepteeLe);

public sealed record AdminGlobalCounts(int TotalEntreprises, int TotalCandidats, int TotalCoachs, IReadOnlyDictionary<string, int> RepartitionModulesActifs);

public sealed record AdminSearchResult(string UserId, string Email, bool EstAdmin, bool EstCandidat, bool EstCoach, IReadOnlyList<string> EntreprisesPossedees);

public sealed record AdminAccountSummary(string UserId, string Email);

public enum AdminActionOutcome
{
    Succes,
    UtilisateurIntrouvable,
    DejaAdmin,
    PasAdmin,
    DernierAdminRestant,
}

public sealed record AdminPaiementView(
    int Id,
    string PlanCode,
    string? ModulesFactures,
    decimal Montant,
    string Devise,
    DateOnly DateReception,
    string Moyen,
    DateOnly PeriodeCouverteDebut,
    DateOnly PeriodeCouverteFin,
    string NotePar,
    DateTimeOffset CreatedAt);

public sealed record AdminAbonnementFacturationView(
    ModuleActivationSubjectType SubjectType,
    int SubjectId,
    string Libelle,
    string PlanCode,
    SubscriptionStatus Status,
    DateTimeOffset? RenewalDate,
    decimal MontantDu,
    string Devise,
    IReadOnlyList<ModuleLigneFacture> DetailModules);

public sealed record AdminModulePrixView(
    string ModuleCode,
    decimal PrixMensuel,
    string Devise,
    bool Facturable);

public sealed record AdminAuditEntryView(int Id, string AdminEmail, string Action, string Cible, DateTimeOffset CreatedAt);

/// <summary>
/// Point d'entrée unique de la zone <c>/admin</c> — lecture seule des métadonnées de compte/abonnement, plus
/// la SEULE action d'écriture autorisée pour ce cycle (promotion/rétrogradation du rôle
/// <see cref="Core.Identity.PlatformRoles.Admin"/>, avec garde du dernier administrateur).
/// </summary>
/// <remarks>
/// Chaque méthode prend explicitement le <see cref="ClaimsPrincipal"/> appelant et vérifie
/// <c>caller.IsInRole(PlatformRoles.Admin)</c> en tout premier — jamais une vérification laissée aux pages
/// Razor (protection au NIVEAU SERVICE, comme demandé). Ce choix (paramètre explicite plutôt que
/// <c>IHttpContextAccessor</c>) rend aussi le service testable directement depuis
/// <c>Spectrometre.Concurrency.Tests</c>, qui n'a pas de contexte HTTP.
/// </remarks>
public interface IAdminService
{
    Task<AdminPagedResult<AdminCompanySummary>> GetEntreprisesAsync(ClaimsPrincipal caller, int page = 1, int pageSize = AdminPaging.DefaultPageSize, string? recherche = null, CancellationToken cancellationToken = default);

    Task<AdminPagedResult<AdminCandidateSummary>> GetCandidatsAsync(ClaimsPrincipal caller, int page = 1, int pageSize = AdminPaging.DefaultPageSize, string? recherche = null, CancellationToken cancellationToken = default);

    Task<AdminPagedResult<AdminCoachSummary>> GetCoachsAsync(ClaimsPrincipal caller, int page = 1, int pageSize = AdminPaging.DefaultPageSize, string? recherche = null, CancellationToken cancellationToken = default);

    Task<AdminPagedResult<AdminCoachingLinkSummary>> GetLiensCoachingAsync(ClaimsPrincipal caller, int page = 1, int pageSize = AdminPaging.DefaultPageSize, string? recherche = null, CancellationToken cancellationToken = default);

    Task<AdminPagedResult<AdminInvitationSummary>> GetInvitationsAsync(ClaimsPrincipal caller, int page = 1, int pageSize = AdminPaging.DefaultPageSize, string? recherche = null, CancellationToken cancellationToken = default);

    Task<AdminGlobalCounts> GetCompteursGlobauxAsync(ClaimsPrincipal caller, CancellationToken cancellationToken = default);

    /// <summary>Recherche par email exact — utile pour retrouver rapidement un compte pour le support. <c>null</c> si aucun compte ne correspond.</summary>
    Task<AdminSearchResult?> RechercherParEmailAsync(ClaimsPrincipal caller, string email, CancellationToken cancellationToken = default);

    Task<AdminPagedResult<AdminAccountSummary>> GetAdministrateursAsync(ClaimsPrincipal caller, int page = 1, int pageSize = AdminPaging.DefaultPageSize, string? recherche = null, CancellationToken cancellationToken = default);

    Task<AdminActionOutcome> PromouvoirAsync(ClaimsPrincipal caller, string targetUserId, CancellationToken cancellationToken = default);

    /// <summary>Refuse (<see cref="AdminActionOutcome.DernierAdminRestant"/>) si <paramref name="targetUserId"/> est le dernier administrateur restant — la plateforme ne doit jamais se retrouver sans aucun admin.</summary>
    Task<AdminActionOutcome> RetrograderAsync(ClaimsPrincipal caller, string targetUserId, CancellationToken cancellationToken = default);

    // ── Actions d'écriture sur l'activation modules (nouveau ce cycle) ────────────────────────────────
    //
    // Réutilisent EXCLUSIVEMENT IModuleRegistry (activation) — mêmes mécanismes que l'inscription/
    // « Ajouter un module », jamais de logique parallèle. Chaque action est historisée
    // (voir GetHistoriqueAsync).

    /// <summary>Active OU désactive <paramref name="moduleCode"/> pour ce sujet — voir <c>IModuleRegistry.SetActiveAsync</c>.</summary>
    Task DefinirActivationModuleAsync(ClaimsPrincipal caller, Core.Modules.ModuleActivationSubjectType subjectType, int subjectId, string moduleCode, bool actif, CancellationToken cancellationToken = default);

    /// <summary>Catalogue complet des modules connus (<c>IModuleRegistry.AllModules</c>) avec l'ensemble de ceux actifs pour ce sujet — support de l'écran de bascule module par module.</summary>
    Task<(IReadOnlyList<string> Tous, IReadOnlyList<string> Actifs)> GetModulesPourSujetAsync(ClaimsPrincipal caller, Core.Modules.ModuleActivationSubjectType subjectType, int subjectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Liste paginée des abonnements facturables. <c>CalculerMontantDuAsync</c> n'est invoqué
    /// QUE pour les lignes de la page demandée (pas pour le total).
    /// </summary>
    Task<AdminPagedResult<AdminAbonnementFacturationView>> GetAbonnementsFacturationAsync(
        ClaimsPrincipal caller,
        int page = 1,
        int pageSize = AdminPaging.DefaultPageSize,
        string? recherche = null,
        CancellationToken cancellationToken = default);

    Task EnregistrerPaiementAsync(
        ClaimsPrincipal caller,
        ModuleActivationSubjectType subjectType,
        int subjectId,
        string planCode,
        decimal montant,
        string devise,
        DateOnly dateReception,
        string moyen,
        DateOnly periodeDebut,
        DateOnly periodeFin,
        CancellationToken cancellationToken = default);

    Task<AdminPagedResult<AdminPaiementView>> GetHistoriquePaiementsAsync(
        ClaimsPrincipal caller,
        ModuleActivationSubjectType subjectType,
        int subjectId,
        int page = 1,
        int pageSize = AdminPaging.DefaultPageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Abonnements Active dont <c>RenewalDate</c> est strictement avant aujourd'hui (UTC) — paginé ; montant calculé uniquement pour la page.</summary>
    Task<AdminPagedResult<AdminAbonnementFacturationView>> GetAbonnementsEnRetardAsync(
        ClaimsPrincipal caller,
        int page = 1,
        int pageSize = AdminPaging.DefaultPageSize,
        string? recherche = null,
        CancellationToken cancellationToken = default);

    Task SuspendreAbonnementAsync(ClaimsPrincipal caller, ModuleActivationSubjectType subjectType, int subjectId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminModulePrixView>> GetModulePrixAsync(ClaimsPrincipal caller, CancellationToken cancellationToken = default);

    Task SetModulePrixAsync(ClaimsPrincipal caller, string moduleCode, decimal prixMensuel, string devise, bool facturable, CancellationToken cancellationToken = default);

    /// <summary>Actions d'écriture Admin, les plus récentes en premier — paginé via Skip/Take EF.</summary>
    Task<AdminPagedResult<AdminAuditEntryView>> GetHistoriqueAsync(
        ClaimsPrincipal caller,
        int page = 1,
        int pageSize = AdminPaging.DefaultPageSize,
        CancellationToken cancellationToken = default);
}
