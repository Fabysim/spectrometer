namespace Spectrometre.Modules.Missions.Services;

/// <summary>Compteurs de synthèse du tableau de bord coach (document Bouchra — cartes de synthèse).</summary>
public sealed record CoachDashboardSynthese(
    int JeunesSuivisActifs,
    int MissionsAValider,
    int DossiersIncomplets,
    int AlertesInvitationsExpirees);

public interface ICoachDashboardService
{
    Task<CoachDashboardSynthese> GetSyntheseAsync(string coachUserId, CancellationToken cancellationToken = default);
}
