using Microsoft.EntityFrameworkCore;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Data;
using Spectrometre.Modules.JeunesPrestataires.Entities;

namespace Spectrometre.Modules.JeunesPrestataires.Services;

public sealed class PlanActionAutoObservationService(
    IDbContextFactory<JeunesPrestatairesDbContext> dbFactory,
    IJeuneProfileService jeuneProfileService,
    ICoachingService coachingService) : IPlanActionAutoObservationService
{
    public async Task<PlanActionAutoObservationView?> GetOrCreateAsync(
        string coachUserId,
        int jeuneProfileId,
        CancellationToken cancellationToken = default)
    {
        var jeune = await AuthorizeCoachAsync(coachUserId, jeuneProfileId, cancellationToken);
        if (jeune is null)
            return null;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.PlansActionAutoObservation.AsNoTracking()
            .FirstOrDefaultAsync(p => p.JeuneProfileId == jeuneProfileId, cancellationToken);
        return ToView(entity, jeuneProfileId);
    }

    /// <summary>
    /// Upsert du plan d'action. Pas de notification jeune : le geste « le coach a relu ton
    /// dossier » est <c>ValiderSyntheseAsync</c> (même page, même lien). Notifier ici en plus
    /// doublerait le message quand le coach enregistre le plan juste après la validation.
    /// </summary>
    public async Task<bool> SaveAsync(
        string coachUserId,
        int jeuneProfileId,
        PlanActionAutoObservationInput input,
        CancellationToken cancellationToken = default)
    {
        var jeune = await AuthorizeCoachAsync(coachUserId, jeuneProfileId, cancellationToken);
        if (jeune is null)
            return false;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.PlansActionAutoObservation
            .FirstOrDefaultAsync(p => p.JeuneProfileId == jeuneProfileId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (entity is null)
        {
            entity = new PlanActionAutoObservation
            {
                JeuneProfileId = jeuneProfileId,
                CreatedAt = now,
            };
            db.PlansActionAutoObservation.Add(entity);
        }

        entity.ObjectifPrincipal = Normalize(input.ObjectifPrincipal);
        entity.PremiereAction = Normalize(input.PremiereAction);
        entity.ResponsableSuivi = Normalize(input.ResponsableSuivi);
        entity.Echeance = input.Echeance;
        entity.IndicateurReussite = Normalize(input.IndicateurReussite);
        entity.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PlanActionAutoObservationView?> GetLectureAsync(
        string requestingUserId,
        int jeuneProfileId,
        CancellationToken cancellationToken = default)
    {
        var jeune = await jeuneProfileService.TryGetByIdAsync(jeuneProfileId, cancellationToken);
        if (jeune is null)
            return null;

        var estJeune = jeune.UserId == requestingUserId;
        if (!estJeune)
        {
            var autorise = await coachingService.GetSuiviUserIdSiAutoriseAsync(
                jeune.UserId, requestingUserId, cancellationToken);
            if (autorise is null)
                return null;
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.PlansActionAutoObservation.AsNoTracking()
            .FirstOrDefaultAsync(p => p.JeuneProfileId == jeuneProfileId, cancellationToken);
        var view = ToView(entity, jeuneProfileId);
        if (estJeune && !view.EstRempli)
            return null;
        return view;
    }

    private async Task<JeuneProfileView?> AuthorizeCoachAsync(
        string coachUserId,
        int jeuneProfileId,
        CancellationToken cancellationToken)
    {
        var jeune = await jeuneProfileService.TryGetByIdAsync(jeuneProfileId, cancellationToken);
        if (jeune is null)
            return null;

        var autorise = await coachingService.GetSuiviUserIdSiAutoriseAsync(jeune.UserId, coachUserId, cancellationToken);
        return autorise is null ? null : jeune;
    }

    private static PlanActionAutoObservationView ToView(PlanActionAutoObservation? entity, int jeuneProfileId) =>
        entity is null
            ? new PlanActionAutoObservationView(null, jeuneProfileId, null, null, null, null, null, null)
            : new PlanActionAutoObservationView(
                entity.Id,
                entity.JeuneProfileId,
                entity.ObjectifPrincipal,
                entity.PremiereAction,
                entity.ResponsableSuivi,
                entity.Echeance,
                entity.IndicateurReussite,
                entity.UpdatedAt);

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return value.Trim();
    }
}
