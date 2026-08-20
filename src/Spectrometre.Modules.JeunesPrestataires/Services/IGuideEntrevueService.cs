namespace Spectrometre.Modules.JeunesPrestataires.Services;

public interface IGuideEntrevueService
{
    /// <summary>
    /// Accès coach→jeune uniquement. Retourne le guide existant ou une vue vide (non persistée).
    /// Null si le coach n'est pas autorisé ou si le profil jeune est introuvable.
    /// </summary>
    Task<GuideEntrevueView?> GetOrCreateAsync(string coachUserId, int jeuneProfileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upsert du guide + notes de peurs. Retourne false si non autorisé.
    /// </summary>
    Task<bool> SaveAsync(
        string coachUserId,
        int jeuneProfileId,
        string? motivations,
        string? freins,
        string? missionsAdaptees,
        string? notesConfidentielles,
        IReadOnlyList<GuideEntrevuePeurNoteInput> peurs,
        CancellationToken cancellationToken = default);
}
