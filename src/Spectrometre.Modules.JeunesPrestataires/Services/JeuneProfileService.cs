using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Data;
using Spectrometre.Core.Email;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Invitations;
using Spectrometre.Core.JeunesPrestataires;
using Spectrometre.Modules.JeunesPrestataires.Data;
using Spectrometre.Modules.JeunesPrestataires.Entities;

namespace Spectrometre.Modules.JeunesPrestataires.Services;

public sealed class JeuneProfileService(
    IDbContextFactory<JeunesPrestatairesDbContext> dbFactory,
    IDbContextFactory<CoreDbContext> coreDbFactory,
    IInvitationService invitationService,
    IResendEmailService resendEmailService) : IJeuneProfileService, IJeunePrestataireInvitationQuery
{
    public async Task<JeuneProfileView?> TryGetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.JeuneProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        return entity is null ? null : ToView(entity);
    }

    public async Task<JeuneProfileView?> TryGetByIdAsync(int jeuneProfileId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.JeuneProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == jeuneProfileId, cancellationToken);
        return entity is null ? null : ToView(entity);
    }

    public async Task<InvitationJeunePrestataireDraft?> TryGetDraftForInvitationAsync(int invitationId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var draft = await db.InvitationsJeunesPrestataires.AsNoTracking()
            .FirstOrDefaultAsync(d => d.InvitationId == invitationId, cancellationToken);
        return draft is null ? null : new InvitationJeunePrestataireDraft(draft.Nom, draft.Prenoms, draft.DateNaissance);
    }

    public async Task<JeuneProfileView> FinaliserDepuisInvitationAsync(
        Invitation invitation,
        string accepteurUserId,
        CancellationToken cancellationToken = default)
    {
        if (invitation.Type != InvitationType.JeunePrestataire)
            throw new InvalidOperationException($"Type d'invitation attendu : {InvitationType.JeunePrestataire}.");

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.JeuneProfiles.FirstOrDefaultAsync(p => p.UserId == accepteurUserId, cancellationToken);
        if (existing is not null)
            return ToView(existing);

        var draft = await db.InvitationsJeunesPrestataires
            .FirstOrDefaultAsync(d => d.InvitationId == invitation.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Brouillon d'invitation jeune introuvable pour l'invitation {invitation.Id}.");

        var profile = new JeuneProfile
        {
            UserId = accepteurUserId,
            Nom = draft.Nom.Trim(),
            Prenoms = draft.Prenoms.Trim(),
            DateNaissance = draft.DateNaissance,
            InvitationId = invitation.Id,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.JeuneProfiles.Add(profile);
        await db.SaveChangesAsync(cancellationToken);
        return ToView(profile);
    }

    public async Task<InviterJeuneResult> InviterJeuneAsync(
        string coachUserId,
        string email,
        string nom,
        string prenoms,
        DateOnly dateNaissance,
        string lienAcceptationBaseUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nom) || string.IsNullOrWhiteSpace(prenoms))
            return new InviterJeuneResult(false, "Erreur_NomPrenomRequis", null, null, false);

        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var invitation = await invitationService.CreerAsync(
            coachUserId,
            email,
            InvitationType.JeunePrestataire,
            contextId: null,
            coreDb,
            cancellationToken);

        db.InvitationsJeunesPrestataires.Add(new InvitationJeunePrestataire
        {
            InvitationId = invitation.Id,
            Nom = nom.Trim(),
            Prenoms = prenoms.Trim(),
            DateNaissance = dateNaissance,
        });
        await db.SaveChangesAsync(cancellationToken);

        var baseUrl = lienAcceptationBaseUrl.TrimEnd('/');
        var lien = $"{baseUrl}/invitations/accepter/{invitation.Token}";

        var coach = await coreDb.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == coachUserId, cancellationToken);
        var coachNomAffiche = ResolveNomAffiche(coach, coachUserId);
        var emailEnvoye = await resendEmailService.SendJeunePrestataireInvitationEmailAsync(
            email,
            coachNomAffiche,
            lien,
            cancellationToken);

        return new InviterJeuneResult(true, null, invitation, lien, emailEnvoye);
    }

    public async Task<IReadOnlyList<JeunePrestataireInvitationPendingView>> GetInvitationsEnvoyeesEnAttenteAsync(
        string coachUserId,
        CancellationToken cancellationToken = default)
    {
        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var invitations = await coreDb.Invitations.AsNoTracking()
            .Where(i => i.EmetteurUserId == coachUserId
                && i.Type == InvitationType.JeunePrestataire
                && i.Statut == InvitationStatus.EnAttente)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

        if (invitations.Count == 0)
            return [];

        var invitationIds = invitations.Select(i => i.Id).ToList();
        var drafts = await db.InvitationsJeunesPrestataires.AsNoTracking()
            .Where(d => invitationIds.Contains(d.InvitationId))
            .ToDictionaryAsync(d => d.InvitationId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        return invitations.Select(i =>
        {
            drafts.TryGetValue(i.Id, out var draft);
            return new JeunePrestataireInvitationPendingView(
                i.Id,
                i.EmailInvite,
                draft?.Nom ?? "",
                draft?.Prenoms ?? "",
                i.CreatedAt,
                i.ExpireLe,
                i.ExpireLe < now);
        }).ToList();
    }

    public async Task<bool> RevoquerInvitationEnvoyeeAsync(
        int invitationId,
        string coachUserId,
        CancellationToken cancellationToken = default)
    {
        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        var invitation = await coreDb.Invitations.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invitationId, cancellationToken);
        if (invitation is null || invitation.Type != InvitationType.JeunePrestataire)
            return false;

        return await invitationService.RevoquerAsync(invitationId, coachUserId, coreDb, cancellationToken);
    }

    public async Task<RenvoyerJeunePrestataireInvitationResult> RenvoyerInvitationAsync(
        int invitationId,
        string requestingCoachUserId,
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var invitation = await coreDb.Invitations
            .FirstOrDefaultAsync(i => i.Id == invitationId, cancellationToken);
        if (invitation is null
            || invitation.EmetteurUserId != requestingCoachUserId
            || invitation.Type != InvitationType.JeunePrestataire
            || invitation.Statut != InvitationStatus.EnAttente)
        {
            return new RenvoyerJeunePrestataireInvitationResult(false, false, false, null);
        }

        var draft = await db.InvitationsJeunesPrestataires
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.InvitationId == invitationId, cancellationToken);
        if (draft is null)
            return new RenvoyerJeunePrestataireInvitationResult(false, false, false, null);

        var now = DateTimeOffset.UtcNow;
        var nouvelleInvitationCreee = false;
        Invitation invitationEffective;

        // Invitation expirée en lecture mais statut encore EnAttente en base : on ne renvoie pas un lien mort —
        // on révoque l'ancienne entrée et on en crée une fraîche (nouveau token, nouvelle ExpireLe).
        if (invitation.ExpireLe < now)
        {
            var revoque = await invitationService.RevoquerAsync(invitationId, requestingCoachUserId, coreDb, cancellationToken);
            if (!revoque)
                return new RenvoyerJeunePrestataireInvitationResult(false, false, false, null);

            invitationEffective = await invitationService.CreerAsync(
                requestingCoachUserId,
                invitation.EmailInvite,
                InvitationType.JeunePrestataire,
                contextId: null,
                coreDb,
                cancellationToken);

            db.InvitationsJeunesPrestataires.Add(new InvitationJeunePrestataire
            {
                InvitationId = invitationEffective.Id,
                Nom = draft.Nom,
                Prenoms = draft.Prenoms,
                DateNaissance = draft.DateNaissance,
            });
            await db.SaveChangesAsync(cancellationToken);
            nouvelleInvitationCreee = true;
        }
        else
        {
            invitationEffective = invitation;
        }

        var lien = $"{baseUrl.TrimEnd('/')}/invitations/accepter/{invitationEffective.Token}";

        var coach = await coreDb.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == requestingCoachUserId, cancellationToken);
        var coachNomAffiche = ResolveNomAffiche(coach, requestingCoachUserId);
        var emailEnvoye = await resendEmailService.SendJeunePrestataireInvitationEmailAsync(
            invitationEffective.EmailInvite,
            coachNomAffiche,
            lien,
            cancellationToken);

        return new RenvoyerJeunePrestataireInvitationResult(
            true,
            emailEnvoye,
            nouvelleInvitationCreee,
            invitationEffective.Id);
    }

    private static string ResolveNomAffiche(ApplicationUser? user, string userId)
    {
        if (user is null)
            return userId;
        var nomComplet = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(nomComplet) ? (user.Email ?? userId) : nomComplet;
    }

    public int CalculerAge(DateOnly dateNaissance)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dateNaissance.Year;
        if (dateNaissance > today.AddYears(-age))
            age--;
        return age;
    }

    public bool EstMineur(DateOnly dateNaissance) => EstMineurStatique(dateNaissance);

    internal static bool EstMineurStatique(DateOnly dateNaissance)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dateNaissance.Year;
        if (dateNaissance > today.AddYears(-age))
            age--;
        return age < 18;
    }

    private static JeuneProfileView ToView(JeuneProfile entity) =>
        new(entity.Id, entity.UserId, entity.Nom, entity.Prenoms, entity.DateNaissance, entity.InvitationId, entity.CreatedAt);
}
