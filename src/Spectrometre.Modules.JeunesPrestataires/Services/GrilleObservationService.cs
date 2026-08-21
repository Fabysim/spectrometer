using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Notifications;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Catalog;
using Spectrometre.Modules.JeunesPrestataires.Data;
using Spectrometre.Modules.JeunesPrestataires.Entities;

namespace Spectrometre.Modules.JeunesPrestataires.Services;

public sealed class GrilleObservationService(
    IDbContextFactory<JeunesPrestatairesDbContext> dbFactory,
    ICoachingService coachingService,
    INotificationService notificationService) : IGrilleObservationService
{
    public async Task<GrilleObservationPageView?> TryGetPageAsync(
        string requestingUserId,
        int? jeuneProfileId,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveAccessAsync(requestingUserId, jeuneProfileId, cancellationToken);
        if (access is null)
            return null;

        var (mode, profile) = access.Value;
        var historique = await GetHistoriqueInternalAsync(profile.Id, cancellationToken);

        return new GrilleObservationPageView(
            mode,
            profile,
            historique,
            CanCreateEvaluation: mode == GrilleObservationAccessMode.Coach);
    }

    public async Task<IReadOnlyList<GrilleObservationHistoriqueItemView>> GetHistoriqueAsync(
        string requestingUserId,
        int jeuneProfileId,
        CancellationToken cancellationToken = default)
    {
        if (await ResolveAccessAsync(requestingUserId, jeuneProfileId, cancellationToken) is null)
            return [];

        return await GetHistoriqueInternalAsync(jeuneProfileId, cancellationToken);
    }

    public async Task<GrilleObservationEvaluationDetailView?> TryGetEvaluationAsync(
        string requestingUserId,
        int evaluationId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var evaluation = await db.GrilleObservationEvaluations.AsNoTracking()
            .Include(e => e.Criteres)
            .FirstOrDefaultAsync(e => e.Id == evaluationId, cancellationToken);
        if (evaluation is null)
            return null;

        var access = await ResolveAccessAsync(requestingUserId, evaluation.JeuneProfileId, cancellationToken);
        if (access is null)
            return null;

        var (mode, profile) = access.Value;
        var criteres = BuildCritereViews(evaluation.Criteres);
        var commentaireGeneral = mode == GrilleObservationAccessMode.Coach
            ? evaluation.CommentaireGeneral
            : null;

        return new GrilleObservationEvaluationDetailView(
            evaluation.Id,
            evaluation.EvalueeLe,
            commentaireGeneral,
            criteres,
            mode,
            profile);
    }

    public async Task<int?> CreerEvaluationAsync(
        string coachUserId,
        int jeuneProfileId,
        IReadOnlyList<GrilleObservationCritereInput> criteres,
        string? commentaireGeneral,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveAccessAsync(coachUserId, jeuneProfileId, cancellationToken);
        if (access is null || access.Value.Mode != GrilleObservationAccessMode.Coach)
            return null;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var evaluation = new GrilleObservationEvaluation
        {
            JeuneProfileId = jeuneProfileId,
            CoachUserId = coachUserId,
            EvalueeLe = now,
            CommentaireGeneral = string.IsNullOrWhiteSpace(commentaireGeneral) ? null : commentaireGeneral.Trim(),
            CreatedAt = now,
        };

        foreach (var input in criteres)
        {
            if (!GrilleObservationCatalog.IsValidKey(input.CritereKey))
                continue;

            if (input.Score is null && string.IsNullOrWhiteSpace(input.Commentaire))
                continue;

            if (input.Score is int score && (score < 1 || score > 5))
                continue;

            evaluation.Criteres.Add(new GrilleObservationCritere
            {
                CritereKey = input.CritereKey,
                Score = input.Score,
                Commentaire = string.IsNullOrWhiteSpace(input.Commentaire) ? null : input.Commentaire.Trim(),
            });
        }

        db.GrilleObservationEvaluations.Add(evaluation);
        await db.SaveChangesAsync(cancellationToken);

        await notificationService.CreerAsync(
            access.Value.Profile.UserId,
            "Nouvelle évaluation",
            "Ton coach a ajouté une évaluation d'observation. Tu peux la voir dans Mes progrès.",
            "/jeune/mes-progres",
            "JeunesPrestataires.GrilleObservationAjoutee",
            cancellationToken);

        return evaluation.Id;
    }

    /// <summary>Même chokepoint que <see cref="AutoObservationService"/>.</summary>
    private async Task<(GrilleObservationAccessMode Mode, JeuneProfileView Profile)?> ResolveAccessAsync(
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
                return (GrilleObservationAccessMode.Jeune, ToView(entity));

            var autorise = await coachingService.GetSuiviUserIdSiAutoriseAsync(entity.UserId, requestingUserId, cancellationToken);
            if (autorise is not null)
                return (GrilleObservationAccessMode.Coach, ToView(entity));

            return null;
        }

        entity = await db.JeuneProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == requestingUserId, cancellationToken);
        return entity is null ? null : (GrilleObservationAccessMode.Jeune, ToView(entity));
    }

    private async Task<IReadOnlyList<GrilleObservationHistoriqueItemView>> GetHistoriqueInternalAsync(
        int jeuneProfileId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var evaluations = await db.GrilleObservationEvaluations.AsNoTracking()
            .Include(e => e.Criteres)
            .Where(e => e.JeuneProfileId == jeuneProfileId)
            .OrderByDescending(e => e.EvalueeLe)
            .ToListAsync(cancellationToken);

        return evaluations.Select(e => new GrilleObservationHistoriqueItemView(
            e.Id,
            e.EvalueeLe,
            ComputeMoyenne(e.Criteres))).ToList();
    }

    private static double? ComputeMoyenne(IEnumerable<GrilleObservationCritere> criteres)
    {
        var scores = criteres.Where(c => c.Score is not null).Select(c => c.Score!.Value).ToList();
        return scores.Count == 0 ? null : scores.Average();
    }

    private static IReadOnlyList<GrilleObservationCritereView> BuildCritereViews(IEnumerable<GrilleObservationCritere> criteres)
    {
        var byKey = criteres.ToDictionary(c => c.CritereKey, StringComparer.Ordinal);
        var views = new List<GrilleObservationCritereView>(GrilleObservationCatalog.AllCriteres.Count);

        foreach (var def in GrilleObservationCatalog.AllCriteres)
        {
            if (byKey.TryGetValue(def.Key, out var row))
                views.Add(new GrilleObservationCritereView(def.Key, row.Score, row.Commentaire));
            else
                views.Add(new GrilleObservationCritereView(def.Key, null, null));
        }

        return views;
    }

    private static JeuneProfileView ToView(JeuneProfile entity) =>
        new(entity.Id, entity.UserId, entity.Nom, entity.Prenoms, entity.DateNaissance, entity.InvitationId, entity.CreatedAt, entity.ProfilAccompagnement);
}
