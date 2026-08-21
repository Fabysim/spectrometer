using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Data;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;

namespace Spectrometre.Core.Notifications;

/// <summary>
/// Préférences + règles de pertinence (voir <see cref="NotificationCategoryCatalog"/>).
/// Pour ajouter une catégorie : étendre le catalogue ET la méthode <see cref="EstPertinenteAsync"/>.
/// </summary>
public sealed class PreferenceNotificationService(
    IDbContextFactory<CoreDbContext> coreDbFactory,
    IModuleRegistry moduleRegistry,
    ICompanyProvisioningService companyProvisioning,
    ICandidateSubjectResolver candidateSubjectResolver,
    ICoachSubjectResolver coachSubjectResolver,
    IParticulierSubjectResolver particulierSubjectResolver) : IPreferenceNotificationService
{
    public async Task<IReadOnlyList<PreferenceNotificationView>> GetPreferencesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        var stored = await db.PreferencesNotification.AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.CategorieCode, p => p.Active, cancellationToken);

        var english = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "fr";
        var result = new List<PreferenceNotificationView>();

        foreach (var def in NotificationCategoryCatalog.All)
        {
            if (!await EstPertinenteAsync(userId, def.CategorieCode, db, cancellationToken))
                continue;

            var active = !stored.TryGetValue(def.CategorieCode, out var pref) || pref;
            result.Add(new PreferenceNotificationView(
                def.CategorieCode,
                english ? def.LibelleEn : def.LibelleFr,
                active));
        }

        return result;
    }

    public async Task SetPreferenceAsync(string userId, string categorieCode, bool active, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(categorieCode);

        await using var db = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.PreferencesNotification
            .FirstOrDefaultAsync(p => p.UserId == userId && p.CategorieCode == categorieCode, cancellationToken);

        if (existing is null)
        {
            db.PreferencesNotification.Add(new PreferenceNotification
            {
                UserId = userId,
                CategorieCode = categorieCode.Trim(),
                Active = active,
            });
        }
        else
        {
            existing.Active = active;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> EstCategorieActiveAsync(string userId, string categorieCode, CancellationToken cancellationToken = default)
    {
        await using var db = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        var pref = await db.PreferencesNotification.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.CategorieCode == categorieCode, cancellationToken);
        return pref is null || pref.Active;
    }

    /// <summary>
    /// Point UNIQUE de pertinence. Nouvelle catégorie = nouveau <c>case</c> ici + entrée catalogue.
    /// </summary>
    private async Task<bool> EstPertinenteAsync(
        string userId,
        string categorieCode,
        CoreDbContext db,
        CancellationToken cancellationToken)
    {
        switch (categorieCode)
        {
            case NotificationCategoryCodes.Coaching:
                // Invitations : tout utilisateur ayant déjà un profil candidat ou coach (sans en créer).
                if (await coachSubjectResolver.TryGetCoachProfileIdAsync(userId, cancellationToken) is not null)
                    return true;
                return await candidateSubjectResolver.TryGetCandidateProfileIdAsync(userId, cancellationToken) is not null;

            case NotificationCategoryCodes.SuiviEmployes:
            {
                var companies = await companyProvisioning.GetCompaniesForUserAsync(userId, db, cancellationToken);
                foreach (var company in companies)
                {
                    var estProprio = await db.UserCompanyLinks.AsNoTracking().AnyAsync(
                        l => l.UserId == userId && l.CompanyId == company.Id && l.Role == CompanyRole.Proprietaire,
                        cancellationToken);
                    if (!estProprio)
                        continue;
                    if (await moduleRegistry.IsActiveAsync(company.Id, "SuiviEmployes", db, cancellationToken))
                        return true;
                }

                return false;
            }

            case NotificationCategoryCodes.GestionDuTemps:
            {
                // Même logique que GestionDuTempsAccessService.HasAccessAsync (candidat / entreprise / coach).
                var candidateId = await candidateSubjectResolver.TryGetCandidateProfileIdAsync(userId, cancellationToken);
                if (candidateId is int cid
                    && await moduleRegistry.IsActiveForCandidateAsync(cid, "GestionDuTemps", db, cancellationToken))
                    return true;

                var companies = await companyProvisioning.GetCompaniesForUserAsync(userId, db, cancellationToken);
                foreach (var company in companies)
                {
                    if (await moduleRegistry.IsActiveAsync(company.Id, "GestionDuTemps", db, cancellationToken))
                        return true;
                }

                var coachId = await coachSubjectResolver.TryGetCoachProfileIdAsync(userId, cancellationToken);
                return coachId is int coachProfileId
                    && await moduleRegistry.IsActiveForCoachAsync(coachProfileId, "GestionDuTemps", db, cancellationToken);
            }

            case NotificationCategoryCodes.Missions:
                // Particulier (publication, validation/refus de publication, confirmation de candidature)
                // ou coach (ex. Missions.ProblemeSignale / Missions.DemandeContact).
                if (await particulierSubjectResolver.TryGetParticulierProfileIdAsync(userId, cancellationToken) is not null)
                    return true;
                return await coachSubjectResolver.TryGetCoachProfileIdAsync(userId, cancellationToken) is not null;

            default:
                return false;
        }
    }
}
