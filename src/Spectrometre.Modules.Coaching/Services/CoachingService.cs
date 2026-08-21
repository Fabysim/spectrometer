using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Ai;
using Spectrometre.Core.Data;
using Spectrometre.Core.Invitations;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Notifications;
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
    IAiSynthesisService aiSynthesisService,
    INotificationService notificationService,
    IJeunePrestatairePresence jeunePrestatairePresence) : ICoachingService
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

        // Jeune prestataire : un seul coach actif. Ne pas ouvrir une 2e demande qui deviendrait un 2e
        // lien Actif à l'acceptation. Les candidats classiques (pas de JeuneProfile) restent multi-coachs.
        if (await JeuneAUnAutreCoachActifAsync(db, suiviUserId, coachUserId, cancellationToken))
            return false;

        db.LiensCoaching.Add(new LienCoaching { SuiviUserId = suiviUserId, CoachUserId = coachUserId });
        await db.SaveChangesAsync(cancellationToken);

        await notificationService.CreerAsync(
            coachUserId,
            "Nouvelle demande de coaching",
            "Une personne souhaite être suivie par vous.",
            "/coach/suivis",
            "Coaching.DemandeRecue",
            cancellationToken);

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

        if (await JeuneAUnAutreCoachActifAsync(db, lien.SuiviUserId, requestingCoachUserId, cancellationToken))
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

    public async Task<bool> TransfererJeunePrestataireAsync(
        string coachSourceUserId,
        string suiviUserId,
        string coachCibleUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(coachSourceUserId)
            || string.IsNullOrWhiteSpace(suiviUserId)
            || string.IsNullOrWhiteSpace(coachCibleUserId)
            || string.Equals(coachSourceUserId, coachCibleUserId, StringComparison.Ordinal))
            return false;

        if (!await jeunePrestatairePresence.EstJeunePrestataireAsync(suiviUserId, cancellationToken))
            return false;

        await using var db = await coachingDbFactory.CreateDbContextAsync(cancellationToken);
        var source = await db.LiensCoaching.FirstOrDefaultAsync(
            l => l.SuiviUserId == suiviUserId
                 && l.CoachUserId == coachSourceUserId
                 && l.Statut == LienCoachingStatut.Actif,
            cancellationToken);
        if (source is null)
            return false;

        var maintenant = DateTimeOffset.UtcNow;
        source.Statut = LienCoachingStatut.Revoque;
        source.ClotureLe = maintenant;

        var cible = await db.LiensCoaching.FirstOrDefaultAsync(
            l => l.SuiviUserId == suiviUserId && l.CoachUserId == coachCibleUserId,
            cancellationToken);
        if (cible is null)
        {
            cible = new LienCoaching
            {
                SuiviUserId = suiviUserId,
                CoachUserId = coachCibleUserId,
                Statut = LienCoachingStatut.Actif,
                AccepteLe = maintenant,
            };
            db.LiensCoaching.Add(cible);
        }
        else
        {
            cible.Statut = LienCoachingStatut.Actif;
            cible.AccepteLe = maintenant;
            cible.ClotureLe = null;
        }

        await db.SaveChangesAsync(cancellationToken);

        await notificationService.CreerAsync(
            suiviUserId,
            "Changement de coach",
            "Votre suivi a été transféré à un autre coach de l'association.",
            "/mon-coach",
            "Coaching.JeuneTransfere",
            cancellationToken);

        await notificationService.CreerAsync(
            coachCibleUserId,
            "Jeune transféré",
            "Un jeune vous a été transféré. Vous en êtes désormais le coach suiveur.",
            $"/coach/suivis/{suiviUserId}/apercu",
            "Coaching.JeuneRecuParTransfert",
            cancellationToken);

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

    public async Task<LienCoachingView?> FinaliserJeunePrestataireDepuisInvitationAsync(Invitation invitation, string accepteurUserId, CancellationToken cancellationToken = default)
    {
        if (invitation.Type != InvitationType.JeunePrestataire)
            throw new InvalidOperationException($"Type d'invitation attendu : {InvitationType.JeunePrestataire}.");

        await using var db = await coachingDbFactory.CreateDbContextAsync(cancellationToken);

        var existantMemeCoach = await db.LiensCoaching.AsNoTracking().FirstOrDefaultAsync(
            l => l.SuiviUserId == accepteurUserId
                 && l.CoachUserId == invitation.EmetteurUserId
                 && l.Statut == LienCoachingStatut.Actif,
            cancellationToken);
        if (existantMemeCoach is not null)
            return ToView(existantMemeCoach);

        // Bloquer plutôt que remplacer silencieusement. Le transfert explicite passe par
        // TransfererJeunePrestataireAsync. Cette méthode est réservée aux invitations jeune.
        var autreActif = await db.LiensCoaching.AsNoTracking().AnyAsync(
            l => l.SuiviUserId == accepteurUserId
                 && l.CoachUserId != invitation.EmetteurUserId
                 && l.Statut == LienCoachingStatut.Actif,
            cancellationToken);
        if (autreActif)
            return null;

        var lien = new LienCoaching
        {
            SuiviUserId = accepteurUserId,
            CoachUserId = invitation.EmetteurUserId,
            Statut = LienCoachingStatut.Actif,
            InvitationId = invitation.Id,
            AccepteLe = DateTimeOffset.UtcNow,
        };
        db.LiensCoaching.Add(lien);
        await db.SaveChangesAsync(cancellationToken);

        return ToView(lien);
    }

    private async Task<bool> JeuneAUnAutreCoachActifAsync(
        CoachingDbContext db,
        string suiviUserId,
        string coachUserId,
        CancellationToken cancellationToken)
    {
        if (!await jeunePrestatairePresence.EstJeunePrestataireAsync(suiviUserId, cancellationToken))
            return false;

        return await db.LiensCoaching.AnyAsync(
            l => l.SuiviUserId == suiviUserId
                 && l.CoachUserId != coachUserId
                 && l.Statut == LienCoachingStatut.Actif,
            cancellationToken);
    }

    private static LienCoachingView ToView(LienCoaching lien) =>
        new(lien.Id, lien.SuiviUserId, lien.CoachUserId, lien.Statut, lien.CreatedAt, lien.AccepteLe);

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
