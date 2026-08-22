namespace Spectrometre.Modules.Missions.Services;

public enum MissionRetourAccessMode
{
    Jeune = 0,
    Coach = 1,
}

public sealed record MissionRetourView(
    int MissionAcceptationId,
    string MissionTitre,
    string? CeQuiSestBienPasse,
    string? CeQuiAEteDifficile,
    string? CeQueJaiAppris,
    string? CeQueJeVeuxAmeliorer,
    MissionRetourAccessMode AccessMode,
    bool PeutEcrire,
    DateTimeOffset? UpdatedAt);

/// <summary>
/// Retour après mission — écriture jeune uniquement (<c>Mission.Statut == Terminee</c>) ;
/// lecture coach suiveur (même chokepoint que grille / auto-observation).
/// </summary>
public interface IMissionRetourService
{
    /// <summary>
    /// Vue existante ou vide (sans créer en base). <c>null</c> si aucun accès
    /// ou mission non terminée.
    /// </summary>
    Task<MissionRetourView?> GetOrCreateAsync(
        string requestingUserId,
        int missionAcceptationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upsert — jeune propriétaire uniquement. <c>false</c> sinon (y compris si le coach appelle).
    /// À la première sauvegarde, notifie le coach référent (<c>Missions.RetourJeuneDisponible</c>)
    /// sans le contenu des champs.
    /// </summary>
    Task<bool> SaveAsync(
        string jeuneUserId,
        int missionAcceptationId,
        string? ceQuiSestBienPasse,
        string? ceQuiAEteDifficile,
        string? ceQueJaiAppris,
        string? ceQueJeVeuxAmeliorer,
        CancellationToken cancellationToken = default);
}
