using Spectrometre.Core.Invitations;
using Spectrometre.Modules.Coaching.Entities;

namespace Spectrometre.Modules.Coaching.Services;

public sealed record LienCoachingView(int Id, string SuiviUserId, string CoachUserId, LienCoachingStatut Statut, DateTimeOffset CreatedAt, DateTimeOffset? AccepteLe);

public sealed record AnamneseCoachingView(string Contenu, bool GenereeParIa, DateTimeOffset UpdatedAt);

/// <summary>
/// Point d'entrée public du module Coaching. <see cref="GetSuiviUserIdSiAutoriseAsync"/> est l'accesseur
/// sécurisé (même rôle que <c>ICompatibiliteService.GetResultatAutorisePourUtilisateurAsync</c>) : jamais
/// d'accès aux données Gestion du temps d'une personne suivie sans être passé par lui d'abord — retourne
/// systématiquement <c>null</c> pour tout lien absent/en attente/révoqué/refusé, jamais une exception.
/// </summary>
public interface ICoachingService
{
    /// <summary>
    /// Seul point d'entrée à utiliser avant toute lecture croisée des données Gestion du temps d'une
    /// personne suivie par un coach : retourne <paramref name="suiviUserId"/> TEL QUEL si un lien
    /// <see cref="LienCoachingStatut.Actif"/> existe entre les deux comptes, <c>null</c> sinon (lien en
    /// attente, révoqué, refusé, ou absent — jamais distingué, pour ne rien révéler à un tiers). L'appelant
    /// doit utiliser la valeur RETOURNÉE (pas son paramètre) comme UserId à transmettre à
    /// <c>IGestionDuTempsService</c>, pour qu'un oubli de vérification ne puisse pas passer inaperçu à la lecture du code.
    /// </summary>
    Task<string?> GetSuiviUserIdSiAutoriseAsync(string suiviUserId, string requestingCoachUserId, CancellationToken cancellationToken = default);

    // ── Côté personne suivie ────────────────────────────────────────────────

    Task<IReadOnlyList<LienCoachingView>> GetLiensPourSuiviAsync(string suiviUserId, CancellationToken cancellationToken = default);

    /// <summary>Demande directe à un coach de l'annuaire (les deux comptes existent déjà) — crée le lien en <see cref="LienCoachingStatut.EnAttente"/>. Ne fait rien si un lien non clos (en attente ou actif) existe déjà pour cette paire, ni si le demandeur est un jeune prestataire qui a déjà un autre coach actif (un seul à la fois ; les candidats classiques restent multi-coachs).</summary>
    Task<bool> DemanderCoachDepuisAnnuaireAsync(string suiviUserId, string coachUserId, CancellationToken cancellationToken = default);

    /// <summary>Invite un coach par email (mécanisme générique — voir <see cref="IInvitationService"/>) — utilisé quand la personne recherchée n'apparaît pas dans l'annuaire. Retourne l'invitation créée (jeton compris) pour affichage du lien à partager.</summary>
    Task<Invitation> InviterCoachParEmailAsync(string suiviUserId, string email, CancellationToken cancellationToken = default);

    /// <summary>Révoque un lien (en attente ou actif) — effectif immédiatement. Seul l'émetteur (la personne suivie) peut révoquer son propre lien.</summary>
    Task<bool> RevoquerAsync(int lienId, string requestingSuiviUserId, CancellationToken cancellationToken = default);

    // ── Côté coach ───────────────────────────────────────────────────────────

    Task<IReadOnlyList<LienCoachingView>> GetLiensPourCoachAsync(string coachUserId, CancellationToken cancellationToken = default);

    Task<bool> AccepterAsync(int lienId, string requestingCoachUserId, CancellationToken cancellationToken = default);

    Task<bool> RefuserAsync(int lienId, string requestingCoachUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transfert immédiat d'un jeune prestataire vers un autre coach : le coach source doit être le
    /// suiveur <see cref="LienCoachingStatut.Actif"/>. Clôture son lien (<see cref="LienCoachingStatut.Revoque"/>,
    /// <c>ClotureLe</c>) et active le lien cible dans la même sauvegarde — jamais deux actifs en parallèle.
    /// La période d'objectifs non archivée (et ses <c>ObjectifCoaching</c>, via la FK période) et
    /// l'anamnèse courante sont rattachées au nouveau lien — pas de duplication. Les périodes déjà
    /// archivées restent sur l'ancien lien (<c>GetArchivesAsync</c> est un historique de relation,
    /// pas un dossier portable). Pas d'étape EnAttente : les coachs de l'association se font déjà
    /// confiance (file de modération partagée). Hors jeune prestataire, ou si le demandeur n'est pas
    /// le coach actif, retourne <c>false</c>.
    /// </summary>
    Task<bool> TransfererJeunePrestataireAsync(
        string coachSourceUserId,
        string suiviUserId,
        string coachCibleUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finalise une invitation Coaching acceptée (voir <c>InvitationAcceptanceService</c>, Host) : crée le
    /// lien directement en <see cref="LienCoachingStatut.Actif"/> — confirmer/finaliser une invitation
    /// sécurisée par jeton EST l'acceptation, aucune étape supplémentaire. L'appelant a la responsabilité
    /// d'avoir déjà vérifié <c>invitation.Type == InvitationType.Coaching</c> et d'avoir activé ProfilCoach
    /// pour <paramref name="accepteurUserId"/> si besoin (voir <c>CoachOnboardingService.CreateCoachAsync</c>,
    /// idempotente).
    /// </summary>
    Task<LienCoachingView> FinaliserDepuisInvitationAsync(Invitation invitation, string accepteurUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finalise une invitation <see cref="InvitationType.JeunePrestataire"/> : coach = émetteur,
    /// accepteur = jeune suivi — sens inverse de <see cref="FinaliserDepuisInvitationAsync"/>.
    /// Ne pas confondre avec l'invitation <see cref="InvitationType.Coaching"/>.
    /// Un jeune n'a qu'un coach actif : si un lien <see cref="LienCoachingStatut.Actif"/> existe déjà
    /// avec un autre coach, retourne <c>null</c> sans créer de second lien (blocage, pas de remplacement
    /// silencieux — le transfert passe par <see cref="TransfererJeunePrestataireAsync"/>). Si le lien avec
    /// le même coach est déjà actif, le renvoie tel quel (idempotent).
    /// </summary>
    Task<LienCoachingView?> FinaliserJeunePrestataireDepuisInvitationAsync(Invitation invitation, string accepteurUserId, CancellationToken cancellationToken = default);

    // ── Anamnèse IA (voir IAiSynthesisService, réutilisé tel quel) ──────────

    Task<AnamneseCoachingView?> GetAnamneseAsync(int lienId, string requestingCoachUserId, CancellationToken cancellationToken = default);

    /// <summary>Génère (ou régénère) l'anamnèse à partir des données Gestion du temps de la personne suivie — même contrôle d'accès que toute lecture croisée, jamais d'appel pour un lien non actif.</summary>
    Task<AnamneseCoachingView?> GenererAnamneseAsync(int lienId, string requestingCoachUserId, CancellationToken cancellationToken = default);
}

