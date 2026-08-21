namespace Spectrometre.Modules.JeunesPrestataires.Services;

public sealed record CharteView(
    bool Acceptee,
    string? NomConfirmation,
    DateTimeOffset? AccepteeLe);

public interface ICharteService
{
    /// <summary><c>null</c> si l'utilisateur n'a pas de profil jeune.</summary>
    Task<CharteView?> GetAsync(string jeuneUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enregistre l'acceptation. Refuse (false) si pas de profil jeune, nom vide, ou déjà acceptée
    /// (pas d'upsert, pas de désacceptation). Notifie le coach référent
    /// (<c>JeunesPrestataires.CharteAcceptee</c>) à la première acceptation.
    /// </summary>
    Task<bool> AccepterAsync(string jeuneUserId, string nomConfirmation, CancellationToken cancellationToken = default);

    Task<bool> EstAccepteeAsync(string jeuneUserId, CancellationToken cancellationToken = default);
}
