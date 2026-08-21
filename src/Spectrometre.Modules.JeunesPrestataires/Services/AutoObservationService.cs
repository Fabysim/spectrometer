using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Notifications;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Catalog;
using Spectrometre.Modules.JeunesPrestataires.Data;
using Spectrometre.Modules.JeunesPrestataires.Entities;

namespace Spectrometre.Modules.JeunesPrestataires.Services;

public sealed class AutoObservationService(
    IDbContextFactory<JeunesPrestatairesDbContext> dbFactory,
    ICoachingService coachingService,
    INotificationService notificationService) : IAutoObservationService
{
    public async Task<AutoObservationPageView?> TryGetPageAsync(
        string requestingUserId,
        int? jeuneProfileId = null,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveAccessAsync(requestingUserId, jeuneProfileId, cancellationToken);
        if (access is null)
            return null;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var progress = await db.AutoObservationSectionProgress.AsNoTracking()
            .Where(p => p.JeuneProfileId == access.Value.Profile.Id)
            .Select(p => new AutoObservationSectionProgressView(p.SectionKey, p.SavedAt))
            .ToListAsync(cancellationToken);

        var syntheseEntity = await db.AutoObservationSynthesesGenerees.AsNoTracking()
            .Where(s => s.JeuneProfileId == access.Value.Profile.Id)
            .OrderByDescending(s => s.GenereeLe)
            .FirstOrDefaultAsync(cancellationToken);

        AutoObservationSyntheseDocument? syntheseDoc = null;
        DateTimeOffset? syntheseLe = syntheseEntity?.GenereeLe;
        DateTimeOffset? syntheseValideeLe = null;
        if (syntheseEntity is not null)
        {
            if (AutoObservationSyntheseDocument.TryParse(syntheseEntity.Contenu, out var parsed))
            {
                syntheseDoc = parsed;
                syntheseValideeLe = syntheseEntity.ValideeLe;
            }
            else
            {
                var json = await PersisterSyntheseAsync(
                    db, access.Value.Profile.Id, access.Value.Profile.ProfilAccompagnement, cancellationToken);
                syntheseDoc = AutoObservationSyntheseDocument.TryParse(json, out var regenere) ? regenere : null;
                syntheseLe = DateTimeOffset.UtcNow;
            }
        }

        var orientationFaite = progress.Any(p => p.SectionKey == AutoObservationOrientationCatalog.SectionKey);
        var orientationAFaire = access.Value.Mode == AutoObservationAccessMode.Jeune && !orientationFaite;

        return new AutoObservationPageView(
            access.Value.Mode,
            access.Value.Profile,
            syntheseDoc,
            syntheseLe,
            progress,
            orientationAFaire,
            syntheseValideeLe);
    }

    public async Task<AutoObservationSectionView?> TryGetSectionAsync(
        string requestingUserId,
        int jeuneProfileId,
        string sectionKey,
        CancellationToken cancellationToken = default)
    {
        var section = AutoObservationCatalog.TryGetSection(sectionKey);
        if (section is null)
            return null;

        var access = await ResolveAccessAsync(requestingUserId, jeuneProfileId, cancellationToken);
        if (access is null || access.Value.Profile.Id != jeuneProfileId)
            return null;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var keys = section.Questions.Select(q => q.Key).ToList();
        var stored = await db.AutoObservationReponses.AsNoTracking()
            .Where(r => r.JeuneProfileId == jeuneProfileId && keys.Contains(r.QuestionKey))
            .ToListAsync(cancellationToken);

        var answers = section.Questions.Select(q =>
        {
            var row = stored.FirstOrDefault(s => s.QuestionKey == q.Key);
            return row is null
                ? new AutoObservationAnswerView(q.Key, null, null, null)
                : new AutoObservationAnswerView(q.Key, row.TextValue, row.NumericValue, row.UpdatedAt);
        }).ToList();

        var savedAt = await db.AutoObservationSectionProgress.AsNoTracking()
            .Where(p => p.JeuneProfileId == jeuneProfileId && p.SectionKey == sectionKey)
            .Select(p => (DateTimeOffset?)p.SavedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var canEdit = CanEditSection(access.Value.Mode, section);

        return new AutoObservationSectionView(
            access.Value.Mode,
            access.Value.Profile,
            sectionKey,
            answers,
            savedAt,
            canEdit);
    }

    public async Task<bool> SaveSectionAsync(
        string requestingUserId,
        int jeuneProfileId,
        string sectionKey,
        IReadOnlyList<AutoObservationAnswerInput> answers,
        CancellationToken cancellationToken = default)
    {
        var section = AutoObservationCatalog.TryGetSection(sectionKey);
        if (section is null || section.IsSynthesisDisplayOnly)
            return false;

        var access = await ResolveAccessAsync(requestingUserId, jeuneProfileId, cancellationToken);
        if (access is null || access.Value.Profile.Id != jeuneProfileId || !CanEditSection(access.Value.Mode, section))
            return false;

        var allowedKeys = section.Questions.Select(q => q.Key).ToHashSet(StringComparer.Ordinal);
        var autreKeys = section.Questions.Where(q => q.AutreKey is not null).Select(q => q.AutreKey!).ToHashSet(StringComparer.Ordinal);
        allowedKeys.UnionWith(autreKeys);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        foreach (var input in answers.Where(a => allowedKeys.Contains(a.QuestionKey)))
        {
            var existing = await db.AutoObservationReponses
                .FirstOrDefaultAsync(r => r.JeuneProfileId == jeuneProfileId && r.QuestionKey == input.QuestionKey, cancellationToken);

            var hasContent = !string.IsNullOrWhiteSpace(input.TextValue) || input.NumericValue.HasValue;

            if (existing is null)
            {
                if (!hasContent)
                    continue;

                db.AutoObservationReponses.Add(new AutoObservationReponse
                {
                    JeuneProfileId = jeuneProfileId,
                    QuestionKey = input.QuestionKey,
                    TextValue = input.TextValue,
                    NumericValue = input.NumericValue,
                    UpdatedAt = DateTimeOffset.UtcNow,
                });
            }
            else
            {
                if (!hasContent)
                {
                    db.AutoObservationReponses.Remove(existing);
                    continue;
                }

                existing.TextValue = input.TextValue;
                existing.NumericValue = input.NumericValue;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        var progress = await db.AutoObservationSectionProgress
            .FirstOrDefaultAsync(p => p.JeuneProfileId == jeuneProfileId && p.SectionKey == sectionKey, cancellationToken);

        if (progress is null)
        {
            db.AutoObservationSectionProgress.Add(new AutoObservationSectionProgress
            {
                JeuneProfileId = jeuneProfileId,
                SectionKey = sectionKey,
                SavedAt = DateTimeOffset.UtcNow,
            });
        }
        else
        {
            progress.SavedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DemanderAideAsync(
        string requestingUserId,
        int jeuneProfileId,
        string sectionKey,
        CancellationToken cancellationToken = default)
    {
        var section = AutoObservationCatalog.TryGetSection(sectionKey);
        if (section is null || section.IsSynthesisDisplayOnly)
            return false;

        var access = await ResolveAccessAsync(requestingUserId, jeuneProfileId, cancellationToken);
        if (access is null || access.Value.Mode != AutoObservationAccessMode.Jeune)
            return false;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var jeune = await db.JeuneProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == jeuneProfileId, cancellationToken);
        if (jeune is null || jeune.UserId != requestingUserId)
            return false;

        var coachId = await FindCoachReferentAsync(jeune.UserId, cancellationToken);
        if (coachId is null)
            return false;

        var jeuneNom = $"{access.Value.Profile.Prenoms} {access.Value.Profile.Nom}".Trim();
        await notificationService.CreerAsync(
            coachId,
            "Besoin d'aide — questionnaire d'auto-observation",
            $"{jeuneNom} demande de l'aide pour la section « {section.Title} ».",
            $"/coach/suivis/{jeune.UserId}/auto-observation?section={sectionKey}",
            "JeunesPrestataires.BesoinAide",
            cancellationToken);

        return true;
    }

    public async Task<string?> RegenererSyntheseAsync(
        string requestingUserId,
        int jeuneProfileId,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveAccessAsync(requestingUserId, jeuneProfileId, cancellationToken);
        if (access is null)
            return null;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await PersisterSyntheseAsync(
            db, jeuneProfileId, access.Value.Profile.ProfilAccompagnement, cancellationToken);
    }

    private async Task<string> PersisterSyntheseAsync(
        JeunesPrestatairesDbContext db,
        int jeuneProfileId,
        ProfilAccompagnement profil,
        CancellationToken cancellationToken)
    {
        var stored = await db.AutoObservationReponses.AsNoTracking()
            .Where(r => r.JeuneProfileId == jeuneProfileId)
            .ToListAsync(cancellationToken);

        var dict = stored.ToDictionary(
            r => r.QuestionKey,
            r => new AutoObservationAnswerView(r.QuestionKey, r.TextValue, r.NumericValue, r.UpdatedAt));

        var derniereGrille = await db.GrilleObservationEvaluations.AsNoTracking()
            .Include(e => e.Criteres)
            .Where(e => e.JeuneProfileId == jeuneProfileId)
            .OrderByDescending(e => e.EvalueeLe)
            .FirstOrDefaultAsync(cancellationToken);
        IReadOnlyList<(string CritereKey, int? Score)>? grille = derniereGrille is null
            ? null
            : derniereGrille.Criteres.Select(c => (c.CritereKey, c.Score)).ToList();

        var json = AutoObservationSyntheseGenerator.Generer(dict, profil, grille).Serialiser();
        var maintenant = DateTimeOffset.UtcNow;

        var existing = await db.AutoObservationSynthesesGenerees
            .FirstOrDefaultAsync(s => s.JeuneProfileId == jeuneProfileId, cancellationToken);

        if (existing is null)
        {
            db.AutoObservationSynthesesGenerees.Add(new AutoObservationSyntheseGeneree
            {
                JeuneProfileId = jeuneProfileId,
                Contenu = json,
                GenereeLe = maintenant,
            });
        }
        else
        {
            existing.Contenu = json;
            existing.GenereeLe = maintenant;
            existing.ValideeLe = null;
            existing.ValideeParCoachUserId = null;
        }

        await db.SaveChangesAsync(cancellationToken);
        return json;
    }

    public async Task<bool> ValiderSyntheseAsync(
        string requestingCoachUserId,
        int jeuneProfileId,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveAccessAsync(requestingCoachUserId, jeuneProfileId, cancellationToken);
        if (access is null
            || access.Value.Profile.Id != jeuneProfileId
            || access.Value.Mode != AutoObservationAccessMode.Coach)
            return false;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var synthese = await db.AutoObservationSynthesesGenerees
            .FirstOrDefaultAsync(s => s.JeuneProfileId == jeuneProfileId, cancellationToken);
        if (synthese is null)
            return false;

        synthese.ValideeLe = DateTimeOffset.UtcNow;
        synthese.ValideeParCoachUserId = requestingCoachUserId;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ASyntheseEnAttenteDeValidationAsync(
        string requestingUserId,
        int jeuneProfileId,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveAccessAsync(requestingUserId, jeuneProfileId, cancellationToken);
        if (access is null || access.Value.Profile.Id != jeuneProfileId)
            return false;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var etat = await db.AutoObservationSynthesesGenerees.AsNoTracking()
            .Where(s => s.JeuneProfileId == jeuneProfileId)
            .Select(s => new { s.ValideeLe })
            .FirstOrDefaultAsync(cancellationToken);
        return etat is not null && etat.ValideeLe is null;
    }

    public async Task<bool> EnregistrerOrientationAsync(
        string requestingUserId,
        int jeuneProfileId,
        IReadOnlyList<AutoObservationAnswerInput> answers,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveAccessAsync(requestingUserId, jeuneProfileId, cancellationToken);
        if (access is null
            || access.Value.Profile.Id != jeuneProfileId
            || access.Value.Mode != AutoObservationAccessMode.Jeune)
            return false;

        var parCle = answers
            .Where(a => AutoObservationOrientationCatalog.QuestionKeys.Contains(a.QuestionKey))
            .GroupBy(a => a.QuestionKey, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Last().TextValue, StringComparer.Ordinal);

        if (AutoObservationOrientationCatalog.Questions.Any(q =>
                !parCle.TryGetValue(q.Key, out var code)
                || !AutoObservationOrientationCatalog.EstReponseValide(q.Key, code)))
            return false;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var dejaFait = await db.AutoObservationSectionProgress.AnyAsync(
            p => p.JeuneProfileId == jeuneProfileId
                 && p.SectionKey == AutoObservationOrientationCatalog.SectionKey,
            cancellationToken);
        if (dejaFait)
            return false;

        var maintenant = DateTimeOffset.UtcNow;
        foreach (var q in AutoObservationOrientationCatalog.Questions)
        {
            db.AutoObservationReponses.Add(new AutoObservationReponse
            {
                JeuneProfileId = jeuneProfileId,
                QuestionKey = q.Key,
                TextValue = parCle[q.Key],
                NumericValue = null,
                UpdatedAt = maintenant,
            });
        }

        db.AutoObservationSectionProgress.Add(new AutoObservationSectionProgress
        {
            JeuneProfileId = jeuneProfileId,
            SectionKey = AutoObservationOrientationCatalog.SectionKey,
            SavedAt = maintenant,
        });

        var jeune = await db.JeuneProfiles.FirstOrDefaultAsync(p => p.Id == jeuneProfileId, cancellationToken);
        if (jeune is null)
            return false;

        jeune.ProfilAccompagnement = AutoObservationOrientationCatalog.SuggereProfil(parCle);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> PasserOrientationAsync(
        string requestingUserId,
        int jeuneProfileId,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveAccessAsync(requestingUserId, jeuneProfileId, cancellationToken);
        if (access is null
            || access.Value.Profile.Id != jeuneProfileId
            || access.Value.Mode != AutoObservationAccessMode.Jeune)
            return false;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var dejaFait = await db.AutoObservationSectionProgress.AnyAsync(
            p => p.JeuneProfileId == jeuneProfileId
                 && p.SectionKey == AutoObservationOrientationCatalog.SectionKey,
            cancellationToken);
        if (dejaFait)
            return true;

        db.AutoObservationSectionProgress.Add(new AutoObservationSectionProgress
        {
            JeuneProfileId = jeuneProfileId,
            SectionKey = AutoObservationOrientationCatalog.SectionKey,
            SavedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static bool CanEditSection(AutoObservationAccessMode mode, AutoObservationSectionDef section)
    {
        if (section.IsSynthesisDisplayOnly)
            return false;

        return mode switch
        {
            AutoObservationAccessMode.Jeune => section.JeuneCanEditAnswers,
            AutoObservationAccessMode.Coach => section.CoachCanEditAnswers,
            _ => false,
        };
    }

    private async Task<(AutoObservationAccessMode Mode, JeuneProfileView Profile)?> ResolveAccessAsync(
        string requestingUserId,
        int? jeuneProfileId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        JeuneProfile? entity;
        if (jeuneProfileId.HasValue)
        {
            entity = await db.JeuneProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == jeuneProfileId.Value, cancellationToken);
            if (entity is null)
                return null;

            if (entity.UserId == requestingUserId)
                return (AutoObservationAccessMode.Jeune, ToView(entity));

            var autorise = await coachingService.GetSuiviUserIdSiAutoriseAsync(entity.UserId, requestingUserId, cancellationToken);
            if (autorise is not null)
                return (AutoObservationAccessMode.Coach, ToView(entity));

            return null;
        }

        entity = await db.JeuneProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == requestingUserId, cancellationToken);
        return entity is null ? null : (AutoObservationAccessMode.Jeune, ToView(entity));
    }

    private async Task<string?> FindCoachReferentAsync(string jeuneUserId, CancellationToken cancellationToken)
    {
        var liens = await coachingService.GetLiensPourSuiviAsync(jeuneUserId, cancellationToken);
        return liens.FirstOrDefault(l => l.Statut == Coaching.Entities.LienCoachingStatut.Actif)?.CoachUserId;
    }

    private static JeuneProfileView ToView(JeuneProfile entity) =>
        new(entity.Id, entity.UserId, entity.Nom, entity.Prenoms, entity.DateNaissance, entity.InvitationId, entity.CreatedAt, entity.ProfilAccompagnement);
}
