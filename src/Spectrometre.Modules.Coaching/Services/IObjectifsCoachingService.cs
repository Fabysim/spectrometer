using Spectrometre.Modules.Coaching.Entities;

namespace Spectrometre.Modules.Coaching.Services;

public sealed record ObjectifCoachingInput(
    int? Id,
    DateOnly Date,
    string Titre,
    string? Moyens,
    AtteinteObjectifCoaching Atteinte,
    string? Observation,
    int? Note);

public sealed record ObjectifCoachingView(
    int Id,
    DateOnly Date,
    string Titre,
    string? Moyens,
    AtteinteObjectifCoaching Atteinte,
    string? Observation,
    int? Note);

public sealed record PeriodeObjectifsCoachingView(
    int Id,
    int LienCoachingId,
    string SuiviUserId,
    DateOnly DateDebut,
    DateOnly DateFin,
    bool Archivee,
    IReadOnlyList<ObjectifCoachingView> Objectifs);

/// <summary>
/// Objectifs de coaching — service dédié (séparé de <see cref="ICoachingService"/>) pour ne pas alourdir
/// le point d'entrée liens/anamnèse. Toutes les méthodes sont réservées au coach propriétaire du lien
/// actif (<c>requestingCoachUserId == LienCoaching.CoachUserId</c> et statut Actif) — direction inverse
/// de <see cref="ICoachingService.GetSuiviUserIdSiAutoriseAsync"/> (qui part du suiviUserId).
/// </summary>
public interface IObjectifsCoachingService
{
    /// <summary>Période non archivée du lien — créée si absente. <c>null</c> si accès refusé.</summary>
    Task<PeriodeObjectifsCoachingView?> GetPeriodeCouranteAsync(int lienId, string requestingCoachUserId, CancellationToken cancellationToken = default);

    /// <summary>Enregistre le brouillon d'objectifs (remplace la liste courante). <c>false</c> si accès refusé ou période absente.</summary>
    Task<bool> SaveObjectifsAsync(int lienId, string requestingCoachUserId, IReadOnlyList<ObjectifCoachingInput> objectifs, CancellationToken cancellationToken = default);

    /// <summary>Archive la période courante après sauvegarde implicite des objectifs fournis (peut être vide = archive l'état DB). Crée ensuite une nouvelle période vide. <c>false</c> si accès refusé.</summary>
    Task<bool> TerminerPeriodeAsync(int lienId, string requestingCoachUserId, IReadOnlyList<ObjectifCoachingInput>? objectifs = null, CancellationToken cancellationToken = default);

    /// <summary>Périodes archivées, plus récentes d'abord. Liste vide si accès refusé.</summary>
    Task<IReadOnlyList<PeriodeObjectifsCoachingView>> GetArchivesAsync(int lienId, string requestingCoachUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Premier lien actif du coach ayant au moins un objectif non clôturé (période courante non archivée,
    /// <see cref="AtteinteObjectifCoaching"/> ≠ Oui). Lecture seule — ne crée pas de période.
    /// <c>null</c> si aucun.
    /// </summary>
    Task<int?> TryGetPremierLienIdAvecObjectifsOuvertsAsync(string coachUserId, CancellationToken cancellationToken = default);
}
