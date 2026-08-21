namespace Spectrometre.Modules.Coaching.Entities;

public enum LienCoachingStatut
{
    EnAttente = 0,
    Actif = 1,
    Refuse = 2,
    Revoque = 3,
}

/// <summary>
/// Lien de coaching entre une personne suivie (<see cref="SuiviUserId"/>) et un coach
/// (<see cref="CoachUserId"/>). En règle générale initié par la personne suivie — jamais par le
/// coach, voir <c>ICoachingService</c>. Exception documentée : le <c>transfert</c> d'un jeune
/// prestataire, déclenché par le coach actif courant (file de modération déjà partagée entre coachs
/// de l'association — confiance déjà accordée). Origines possibles, distinguées par
/// <see cref="InvitationId"/> :
/// <list type="bullet">
/// <item><description>Demande depuis l'annuaire (<see cref="InvitationId"/> null) : les deux comptes
/// existent déjà, le lien est créé directement en <see cref="LienCoachingStatut.EnAttente"/>, le coach doit
/// l'accepter depuis « Mes personnes suivies ».</description></item>
/// <item><description>Invitation par email (<see cref="InvitationId"/> renseigné, référence molle vers
/// <c>Invitation.Id</c> côté Core — jamais une contrainte de clé étrangère, même principe que
/// <c>ModuleActivation.SubjectId</c>) : le lien est créé directement en
/// <see cref="LienCoachingStatut.Actif"/> au moment où l'invité confirme/finalise son compte — accepter le
/// lien d'invitation sécurisé EST l'acceptation, pas d'étape supplémentaire.</description></item>
/// <item><description>Transfert immédiat par le coach actif courant (jeune prestataire uniquement,
/// <see cref="InvitationId"/> null) : l'ancien lien est clos en <see cref="LienCoachingStatut.Revoque"/>
/// et le nouveau est créé/réactivé en <see cref="LienCoachingStatut.Actif"/> dans la même sauvegarde.
/// Exception volontaire à l'initiation par la personne suivie — voir
/// <c>ICoachingService.TransfererJeunePrestataireAsync</c>.</description></item>
/// </list>
/// </summary>
public sealed class LienCoaching
{
    public int Id { get; set; }
    public required string SuiviUserId { get; set; }
    public required string CoachUserId { get; set; }
    public LienCoachingStatut Statut { get; set; } = LienCoachingStatut.EnAttente;
    public int? InvitationId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AccepteLe { get; set; }
    public DateTimeOffset? ClotureLe { get; set; }
}
