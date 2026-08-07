using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Data;
using Spectrometre.Core.Invitations;
using Spectrometre.Modules.Coaching.Data;
using Spectrometre.Modules.Coaching.Entities;
using Spectrometre.Modules.GestionDuTemps.Services;

namespace Spectrometre.Modules.Coaching.Services;

/// <summary>
/// <see cref="IDbContextFactory{TContext}"/> partout (jamais un DbContext scopé injecté) — même raison que
/// partout ailleurs dans ce projet : une instance fraîche par appel élimine toute classe de bug liée à deux
/// opérations concurrentes sur un même DbContext.
/// </summary>
public sealed class CoachingService(
    IDbContextFactory<CoachingDbContext> coachingDbFactory,
    IDbContextFactory<CoreDbContext> coreDbFactory,
    IInvitationService invitationService,
    IGestionDuTempsService gestionDuTempsService,
    IAiSynthesisService aiSynthesisService) : ICoachingService
{
    public async Task<string?> GetSuiviUserIdSiAutoriseAsync(string suiviUserId, string requestingCoachUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await coachingDbFactory.CreateDbContextAsync(cancellationToken);
        var actif = await db.LiensCoaching.AsNoTracking().AnyAsync(
            l => l.SuiviUserId == suiviUserId && l.CoachUserId == requestingCoachUserId && l.Statut == LienCoachingStatut.Actif,
            cancellationToken);
        return actif ? suiviUserId : null;
    }

    public async Task<IReadOnlyList<LienCoachingView>> GetLiensPourSuiviAsync(string suiviUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await coachingDbFactory.CreateDbContextAsync(cancellationToken);
        return await db.LiensCoaching.AsNoTracking()
            .Where(l => l.SuiviUserId == suiviUserId)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new LienCoachingView(l.Id, l.SuiviUserId, l.CoachUserId, l.Statut, l.CreatedAt, l.AccepteLe))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DemanderCoachDepuisAnnuaireAsync(string suiviUserId, string coachUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await coachingDbFactory.CreateDbContextAsync(cancellationToken);

        var existeDeja = await db.LiensCoaching.AnyAsync(
            l => l.SuiviUserId == suiviUserId && l.CoachUserId == coachUserId
                 && (l.Statut == LienCoachingStatut.EnAttente || l.Statut == LienCoachingStatut.Actif),
            cancellationToken);
        if (existeDeja)
            return false;

        db.LiensCoaching.Add(new LienCoaching { SuiviUserId = suiviUserId, CoachUserId = coachUserId });
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<Invitation> InviterCoachParEmailAsync(string suiviUserId, string email, CancellationToken cancellationToken = default)
    {
        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        return await invitationService.CreerAsync(suiviUserId, email, InvitationType.Coaching, contextId: null, coreDb, cancellationToken);
    }

    public async Task<bool> RevoquerAsync(int lienId, string requestingSuiviUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await coachingDbFactory.CreateDbContextAsync(cancellationToken);
        var lien = await db.LiensCoaching.FirstOrDefaultAsync(l => l.Id == lienId, cancellationToken);
        if (lien is null || lien.SuiviUserId != requestingSuiviUserId)
            return false;
        if (lien.Statut is LienCoachingStatut.Revoque or LienCoachingStatut.Refuse)
            return false;

        lien.Statut = LienCoachingStatut.Revoque;
        lien.ClotureLe = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<LienCoachingView>> GetLiensPourCoachAsync(string coachUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await coachingDbFactory.CreateDbContextAsync(cancellationToken);
        return await db.LiensCoaching.AsNoTracking()
            .Where(l => l.CoachUserId == coachUserId)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new LienCoachingView(l.Id, l.SuiviUserId, l.CoachUserId, l.Statut, l.CreatedAt, l.AccepteLe))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> AccepterAsync(int lienId, string requestingCoachUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await coachingDbFactory.CreateDbContextAsync(cancellationToken);
        var lien = await db.LiensCoaching.FirstOrDefaultAsync(l => l.Id == lienId, cancellationToken);
        if (lien is null || lien.CoachUserId != requestingCoachUserId || lien.Statut != LienCoachingStatut.EnAttente)
            return false;

        lien.Statut = LienCoachingStatut.Actif;
        lien.AccepteLe = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RefuserAsync(int lienId, string requestingCoachUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await coachingDbFactory.CreateDbContextAsync(cancellationToken);
        var lien = await db.LiensCoaching.FirstOrDefaultAsync(l => l.Id == lienId, cancellationToken);
        if (lien is null || lien.CoachUserId != requestingCoachUserId || lien.Statut != LienCoachingStatut.EnAttente)
            return false;

        lien.Statut = LienCoachingStatut.Refuse;
        lien.ClotureLe = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<LienCoachingView> FinaliserDepuisInvitationAsync(Invitation invitation, string accepteurUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await coachingDbFactory.CreateDbContextAsync(cancellationToken);

        var lien = new LienCoaching
        {
            SuiviUserId = invitation.EmetteurUserId,
            CoachUserId = accepteurUserId,
            Statut = LienCoachingStatut.Actif,
            InvitationId = invitation.Id,
            AccepteLe = DateTimeOffset.UtcNow,
        };
        db.LiensCoaching.Add(lien);
        await db.SaveChangesAsync(cancellationToken);

        return new LienCoachingView(lien.Id, lien.SuiviUserId, lien.CoachUserId, lien.Statut, lien.CreatedAt, lien.AccepteLe);
    }

    public async Task<AnamneseCoachingView?> GetAnamneseAsync(int lienId, string requestingCoachUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await coachingDbFactory.CreateDbContextAsync(cancellationToken);
        var lien = await db.LiensCoaching.AsNoTracking().FirstOrDefaultAsync(l => l.Id == lienId, cancellationToken);
        if (lien is null || lien.CoachUserId != requestingCoachUserId || lien.Statut != LienCoachingStatut.Actif)
            return null;

        var anamnese = await db.AnamnesesCoaching.AsNoTracking().FirstOrDefaultAsync(a => a.LienCoachingId == lienId, cancellationToken);
        return anamnese is null ? null : new AnamneseCoachingView(anamnese.Contenu, anamnese.GenereeParIa, anamnese.UpdatedAt);
    }

    public async Task<AnamneseCoachingView?> GenererAnamneseAsync(int lienId, string requestingCoachUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await coachingDbFactory.CreateDbContextAsync(cancellationToken);
        var lien = await db.LiensCoaching.FirstOrDefaultAsync(l => l.Id == lienId, cancellationToken);
        if (lien is null || lien.CoachUserId != requestingCoachUserId || lien.Statut != LienCoachingStatut.Actif)
            return null;

        var profil = await gestionDuTempsService.GetProfilPsychosocialAsync(lien.SuiviUserId, cancellationToken);
        var reflexion = await gestionDuTempsService.GetReflexionConscienteAsync(lien.SuiviUserId, cancellationToken);
        var synthese = await gestionDuTempsService.GetSyntheseAsync(lien.SuiviUserId, cancellationToken);

        var english = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "fr";

        string contenu;
        bool genereeParIa;
        if (profil is null)
        {
            contenu = AnamneseNarrativeBuilder.BuildFallback(null, synthese, english);
            genereeParIa = false;
        }
        else
        {
            var systemPrompt = AnamneseNarrativeBuilder.BuildSystemPrompt(english);
            var userPrompt = AnamneseNarrativeBuilder.BuildUserPrompt(profil, reflexion, synthese);
            var (output, error) = await aiSynthesisService.GenererTexteAsync(systemPrompt, userPrompt, cancellationToken);

            if (error is not null || string.IsNullOrWhiteSpace(output))
            {
                contenu = AnamneseNarrativeBuilder.BuildFallback(profil, synthese, english);
                genereeParIa = false;
            }
            else
            {
                contenu = output.Trim();
                genereeParIa = true;
            }
        }

        var anamnese = await db.AnamnesesCoaching.FirstOrDefaultAsync(a => a.LienCoachingId == lienId, cancellationToken);
        if (anamnese is null)
        {
            anamnese = new AnamneseCoaching { LienCoachingId = lienId, Contenu = contenu };
            db.AnamnesesCoaching.Add(anamnese);
        }

        anamnese.Contenu = contenu;
        anamnese.GenereeParIa = genereeParIa;
        anamnese.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new AnamneseCoachingView(anamnese.Contenu, anamnese.GenereeParIa, anamnese.UpdatedAt);
    }
}
