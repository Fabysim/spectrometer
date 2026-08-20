using Microsoft.EntityFrameworkCore;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Catalog;
using Spectrometre.Modules.JeunesPrestataires.Data;
using Spectrometre.Modules.JeunesPrestataires.Entities;

namespace Spectrometre.Modules.JeunesPrestataires.Services;

/// <summary>
/// Guide d'entrevue — accès coach uniquement via <see cref="ICoachingService.GetSuiviUserIdSiAutoriseAsync"/>.
/// Document vivant (upsert), jamais visible du jeune.
/// </summary>
public sealed class GuideEntrevueService(
    IDbContextFactory<JeunesPrestatairesDbContext> dbFactory,
    IJeuneProfileService jeuneProfileService,
    ICoachingService coachingService) : IGuideEntrevueService
{
    public async Task<GuideEntrevueView?> GetOrCreateAsync(
        string coachUserId,
        int jeuneProfileId,
        CancellationToken cancellationToken = default)
    {
        var jeune = await AuthorizeCoachAsync(coachUserId, jeuneProfileId, cancellationToken);
        if (jeune is null)
            return null;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.GuidesEntrevue.AsNoTracking()
            .Include(g => g.PeurNotes)
            .FirstOrDefaultAsync(g => g.JeuneProfileId == jeuneProfileId, cancellationToken);

        return ToView(entity, jeune);
    }

    public async Task<bool> SaveAsync(
        string coachUserId,
        int jeuneProfileId,
        string? motivations,
        string? freins,
        string? missionsAdaptees,
        string? notesConfidentielles,
        IReadOnlyList<GuideEntrevuePeurNoteInput> peurs,
        CancellationToken cancellationToken = default)
    {
        var jeune = await AuthorizeCoachAsync(coachUserId, jeuneProfileId, cancellationToken);
        if (jeune is null)
            return false;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.GuidesEntrevue
            .Include(g => g.PeurNotes)
            .FirstOrDefaultAsync(g => g.JeuneProfileId == jeuneProfileId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (entity is null)
        {
            entity = new GuideEntrevue
            {
                JeuneProfileId = jeuneProfileId,
                CreatedAt = now,
            };
            db.GuidesEntrevue.Add(entity);
        }

        entity.Motivations = Normalize(motivations);
        entity.Freins = Normalize(freins);
        entity.MissionsAdaptees = Normalize(missionsAdaptees);
        entity.NotesConfidentielles = Normalize(notesConfidentielles);
        entity.UpdatedAt = now;

        var notesByKey = peurs
            .Where(p => GuideEntrevueCatalog.IsValidPeurKey(p.PeurKey))
            .GroupBy(p => p.PeurKey, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => Normalize(g.Last().NoteCoach), StringComparer.Ordinal);

        foreach (var peurDef in GuideEntrevueCatalog.Peurs)
        {
            notesByKey.TryGetValue(peurDef.Key, out var note);
            var existing = entity.PeurNotes.FirstOrDefault(n => n.PeurKey == peurDef.Key);
            if (existing is null)
            {
                entity.PeurNotes.Add(new GuideEntrevuePeurNote
                {
                    PeurKey = peurDef.Key,
                    NoteCoach = note,
                });
            }
            else
            {
                existing.NoteCoach = note;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<JeuneProfileView?> AuthorizeCoachAsync(
        string coachUserId,
        int jeuneProfileId,
        CancellationToken cancellationToken)
    {
        var jeune = await jeuneProfileService.TryGetByIdAsync(jeuneProfileId, cancellationToken);
        if (jeune is null)
            return null;

        // Coach→jeune uniquement — même le jeune propriétaire est refusé (pas de mode Jeune).
        var autorise = await coachingService.GetSuiviUserIdSiAutoriseAsync(jeune.UserId, coachUserId, cancellationToken);
        return autorise is null ? null : jeune;
    }

    private static GuideEntrevueView ToView(GuideEntrevue? entity, JeuneProfileView jeune)
    {
        var notesByKey = entity?.PeurNotes.ToDictionary(n => n.PeurKey, n => n.NoteCoach, StringComparer.Ordinal)
            ?? new Dictionary<string, string?>(StringComparer.Ordinal);

        var peurs = GuideEntrevueCatalog.Peurs
            .Select(p => new GuideEntrevuePeurNoteView(p.Key, notesByKey.GetValueOrDefault(p.Key)))
            .ToList();

        return new GuideEntrevueView(
            entity?.Id,
            jeune.Id,
            jeune.Nom,
            jeune.Prenoms,
            entity?.Motivations,
            entity?.Freins,
            entity?.MissionsAdaptees,
            entity?.NotesConfidentielles,
            peurs,
            entity?.UpdatedAt);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
