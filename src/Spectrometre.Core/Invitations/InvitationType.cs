namespace Spectrometre.Core.Invitations;

/// <summary>
/// Type d'invitation — détermine quel profil est activé pour l'invité une fois l'invitation acceptée
/// (voir <c>InvitationAcceptanceService</c> côté Host, seul endroit qui interprète cette valeur pour
/// déclencher l'activation de profil appropriée). Conçu pour rester extensible sans refonte : ajouter un
/// futur type (ex. <c>CompanyManager</c>, pour inviter un manager sous une entreprise) ne demande qu'une
/// nouvelle valeur d'énumération — <see cref="Invitation.ContextId"/> existe déjà pour porter l'identifiant
/// contextuel qu'un tel type nécessiterait (ex. la CompanyId à laquelle rattacher le manager), inutilisé par
/// <see cref="Coaching"/>.
/// </summary>
public enum InvitationType
{
    Coaching = 0,
}
