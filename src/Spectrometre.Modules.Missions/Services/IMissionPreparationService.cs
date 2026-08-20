namespace Spectrometre.Modules.Missions.Services;

public sealed record MissionPreparationItemView(string ItemKey, bool Coche);

public sealed record MissionPreparationView(
    int MissionAcceptationId,
    string MissionTitre,
    IReadOnlyList<MissionPreparationItemView> Items);

/// <summary>
/// Checklist « préparation avant mission » — accès jeune propriétaire uniquement,
/// uniquement si l'acceptation est <c>ValideeParCoach</c>.
/// </summary>
public interface IMissionPreparationService
{
    /// <summary>
    /// État des 6 items. <c>null</c> si accès refusé (pas propriétaire, acceptation
    /// non validée, ou introuvable).
    /// </summary>
    Task<MissionPreparationView?> GetPreparationAsync(
        string jeuneUserId,
        int missionAcceptationId,
        CancellationToken cancellationToken = default);

    /// <summary>Upsert d'une case. <c>false</c> si accès refusé ou clé catalogue invalide.</summary>
    Task<bool> ToggleItemPreparationAsync(
        string jeuneUserId,
        int missionAcceptationId,
        string itemKey,
        bool coche,
        CancellationToken cancellationToken = default);
}
