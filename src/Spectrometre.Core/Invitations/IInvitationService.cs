using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Data;

namespace Spectrometre.Core.Invitations;

/// <summary>
/// Émission/consultation des invitations génériques par email (voir <see cref="Invitation"/>). Ne connaît
/// rien du type d'invitation qu'il manipule : c'est à l'appelant (ex. le module Coaching, ou plus tard un
/// futur parcours manager) d'interpréter <see cref="Invitation.Type"/> une fois l'invitation acceptée pour
/// finaliser le lien métier correspondant.
/// </summary>
public interface IInvitationService
{
    Task<Invitation> CreerAsync(string emetteurUserId, string emailInvite, InvitationType type, int? contextId, CoreDbContext db, CancellationToken cancellationToken = default);

    /// <summary>Retourne l'invitation quel que soit son statut (pour affichage/liste côté émetteur) — jamais utilisé pour décider une acceptation, voir <see cref="ObtenirValidePourAcceptationAsync"/>.</summary>
    Task<Invitation?> ObtenirParTokenAsync(string token, CoreDbContext db, CancellationToken cancellationToken = default);

    /// <summary>
    /// Seul point d'entrée à utiliser avant d'accepter une invitation : retourne l'invitation SEULEMENT SI
    /// son statut est <see cref="InvitationStatus.EnAttente"/> ET qu'elle n'est pas expirée — sinon <c>null</c>
    /// (jamais d'exception). Marque au passage le statut <see cref="InvitationStatus.Expiree"/> en base si la
    /// date d'expiration est dépassée, pour que le statut stocké reflète la réalité au lieu d'être recalculé
    /// indéfiniment "en attente".
    /// </summary>
    Task<Invitation?> ObtenirValidePourAcceptationAsync(string token, CoreDbContext db, CancellationToken cancellationToken = default);

    Task MarquerAccepteeAsync(int invitationId, CoreDbContext db, CancellationToken cancellationToken = default);

    /// <summary>Révoque une invitation en attente — seul l'émetteur peut révoquer la sienne ; ne fait rien (retourne false) sinon.</summary>
    Task<bool> RevoquerAsync(int invitationId, string requestingUserId, CoreDbContext db, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Invitation>> ObtenirEmisesParAsync(string emetteurUserId, InvitationType type, CoreDbContext db, CancellationToken cancellationToken = default);
}

public sealed class InvitationService : IInvitationService
{
    private static readonly TimeSpan DureeDeValidite = TimeSpan.FromDays(7);

    public async Task<Invitation> CreerAsync(string emetteurUserId, string emailInvite, InvitationType type, int? contextId, CoreDbContext db, CancellationToken cancellationToken = default)
    {
        var invitation = new Invitation
        {
            EmetteurUserId = emetteurUserId,
            EmailInvite = emailInvite.Trim().ToLowerInvariant(),
            Type = type,
            ContextId = contextId,
            Token = GenererToken(),
            ExpireLe = DateTimeOffset.UtcNow.Add(DureeDeValidite),
        };

        db.Invitations.Add(invitation);
        await db.SaveChangesAsync(cancellationToken);
        return invitation;
    }

    public Task<Invitation?> ObtenirParTokenAsync(string token, CoreDbContext db, CancellationToken cancellationToken = default) =>
        db.Invitations.FirstOrDefaultAsync(i => i.Token == token, cancellationToken);

    public async Task<Invitation?> ObtenirValidePourAcceptationAsync(string token, CoreDbContext db, CancellationToken cancellationToken = default)
    {
        var invitation = await db.Invitations.FirstOrDefaultAsync(i => i.Token == token, cancellationToken);
        if (invitation is null)
            return null;

        if (invitation.Statut == InvitationStatus.EnAttente && invitation.ExpireLe < DateTimeOffset.UtcNow)
        {
            invitation.Statut = InvitationStatus.Expiree;
            await db.SaveChangesAsync(cancellationToken);
        }

        return invitation.Statut == InvitationStatus.EnAttente ? invitation : null;
    }

    public async Task MarquerAccepteeAsync(int invitationId, CoreDbContext db, CancellationToken cancellationToken = default)
    {
        var invitation = await db.Invitations.FirstOrDefaultAsync(i => i.Id == invitationId, cancellationToken)
            ?? throw new InvalidOperationException($"Invitation introuvable : {invitationId}.");

        invitation.Statut = InvitationStatus.Acceptee;
        invitation.AccepteeLe = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RevoquerAsync(int invitationId, string requestingUserId, CoreDbContext db, CancellationToken cancellationToken = default)
    {
        var invitation = await db.Invitations.FirstOrDefaultAsync(i => i.Id == invitationId, cancellationToken);
        if (invitation is null || invitation.EmetteurUserId != requestingUserId || invitation.Statut != InvitationStatus.EnAttente)
            return false;

        invitation.Statut = InvitationStatus.Revoquee;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<Invitation>> ObtenirEmisesParAsync(string emetteurUserId, InvitationType type, CoreDbContext db, CancellationToken cancellationToken = default) =>
        await db.Invitations
            .Where(i => i.EmetteurUserId == emetteurUserId && i.Type == type)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

    /// <summary>256 bits d'entropie, encodage Base64Url (pas de <c>+</c>/<c>/</c>/padding — sûr à coller tel quel dans une URL).</summary>
    private static string GenererToken()
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
