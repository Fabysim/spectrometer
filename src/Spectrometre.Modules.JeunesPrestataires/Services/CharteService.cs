using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Notifications;
using Spectrometre.Modules.Coaching.Entities;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Data;
using Spectrometre.Modules.JeunesPrestataires.Entities;

namespace Spectrometre.Modules.JeunesPrestataires.Services;

public sealed class CharteService(
    IDbContextFactory<JeunesPrestatairesDbContext> dbFactory,
    IJeuneProfileService jeuneProfileService,
    ICoachingService coachingService,
    INotificationService notificationService) : ICharteService
{
    public async Task<CharteView?> GetAsync(string jeuneUserId, CancellationToken cancellationToken = default)
    {
        var jeune = await jeuneProfileService.TryGetByUserIdAsync(jeuneUserId, cancellationToken);
        if (jeune is null)
            return null;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var row = await db.CharteAcceptations.AsNoTracking()
            .FirstOrDefaultAsync(c => c.JeuneProfileId == jeune.Id, cancellationToken);

        if (row is null)
            return new CharteView(false, null, null);

        return new CharteView(true, row.NomConfirmation, row.AccepteeLe);
    }

    public async Task<bool> AccepterAsync(
        string jeuneUserId,
        string nomConfirmation,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nomConfirmation))
            return false;

        var jeune = await jeuneProfileService.TryGetByUserIdAsync(jeuneUserId, cancellationToken);
        if (jeune is null)
            return false;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var existe = await db.CharteAcceptations.AnyAsync(c => c.JeuneProfileId == jeune.Id, cancellationToken);
        if (existe)
            return false;

        var now = DateTimeOffset.UtcNow;
        db.CharteAcceptations.Add(new CharteAcceptation
        {
            JeuneProfileId = jeune.Id,
            NomConfirmation = nomConfirmation.Trim(),
            AccepteeLe = now,
            CreatedAt = now,
        });
        await db.SaveChangesAsync(cancellationToken);
        await NotifierCoachCharteAccepteeAsync(jeune, cancellationToken);
        return true;
    }

    private async Task NotifierCoachCharteAccepteeAsync(
        JeuneProfileView jeune,
        CancellationToken cancellationToken)
    {
        var liens = await coachingService.GetLiensPourSuiviAsync(jeune.UserId, cancellationToken);
        var coachId = liens.FirstOrDefault(l => l.Statut == LienCoachingStatut.Actif)?.CoachUserId;
        if (coachId is null)
            return;

        var jeuneNom = $"{jeune.Prenoms} {jeune.Nom}".Trim();
        await notificationService.CreerAsync(
            coachId,
            "Charte acceptée",
            $"{jeuneNom} a accepté la charte et peut désormais accepter des missions.",
            $"/coach/suivis/{jeune.UserId}/apercu",
            "JeunesPrestataires.CharteAcceptee",
            cancellationToken);
    }

    public async Task<bool> EstAccepteeAsync(string jeuneUserId, CancellationToken cancellationToken = default)
    {
        var view = await GetAsync(jeuneUserId, cancellationToken);
        return view is { Acceptee: true };
    }
}
