namespace Spectrometre.Core.Invitations;

/// <summary>
/// Type d'invitation — détermine quel profil est activé pour l'invité une fois l'invitation acceptée
/// (voir <c>InvitationAcceptanceService</c> côté Host, seul endroit qui interprète cette valeur pour
/// déclencher l'activation de profil appropriée). Conçu pour rester extensible sans refonte : ajouter un
/// futur type (ex. <c>CompanyEmploye</c>, pour inviter un employé sous une entreprise) ne demande qu'une
/// nouvelle valeur d'énumération — <see cref="Invitation.ContextId"/> existe déjà pour porter l'identifiant
/// contextuel qu'un tel type nécessiterait (ex. la CompanyId à laquelle rattacher l'employé), inutilisé par
/// <see cref="Coaching"/>.
/// </summary>
public enum InvitationType
{
    Coaching = 0,

    /// <summary>Invitation d'un employé sur une entreprise EXISTANTE — <see cref="Invitation.ContextId"/> porte la <c>CompanyId</c> concernée (voir sa remarque).</summary>
    CompanyEmploye = 1,

    /// <summary>
    /// Invitation d'un candidat à postuler sur un poste — <see cref="Invitation.ContextId"/> porte le
    /// <c>PosteId</c> concerné (même pattern que <see cref="CompanyEmploye"/> avec <c>CompanyId</c>).
    /// À l'acceptation, un <c>CandidateProfile</c> est résolu/créé pour l'utilisateur et une
    /// <c>Candidature</c> est créée via <c>IPosteService.PostulerAsync</c> (jamais de compte créé
    /// au nom d'un tiers).
    /// </summary>
    CandidaturePoste = 2,
}
