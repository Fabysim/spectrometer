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
/// le point d'entrée liens/anamnèse. Les méthodes d'écriture et <see cref="GetPeriodeCouranteAsync"/>
/// restent réservées au coach propriétaire du lien actif. <see cref="GetPeriodeCourantePourJeuneAsync"/>
/// est la lecture seule du jeune sur les mêmes entités (pas de duplication de modèle).
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

    /// <summary>
    /// Lecture seule pour le jeune connecté : période non archivée de son lien actif.
    /// Ne crée pas de période (contrairement à <see cref="GetPeriodeCouranteAsync"/>).
    /// Pas de circuit « choisir / valider » ce cycle — l'édition reste au coach.
    /// <c>null</c> si aucun lien actif ou aucune période en cours.
    /// </summary>
    Task<PeriodeObjectifsCoachingJeuneView?> GetPeriodeCourantePourJeuneAsync(
        string jeuneUserId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Vue jeune : <c>Titre</c>, <c>Moyens</c>, <c>Atteinte</c> uniquement.
/// <c>Observation</c> / <c>Note</c> exclus — sur la page coach ce sont un score 0–100 et un
/// commentaire d'évaluation, pas le libellé de l'objectif (même principe que
/// <c>GrilleObservationEvaluation.CommentaireGeneral</c>, jamais exposé au jeune).
/// Rien dans le code ne les marque « confidentiels », mais les exposer reviendrait à montrer
/// le jugement du coach, hors périmètre « objectif + moyen + atteinte ».
/// </summary>
public sealed record ObjectifCoachingJeuneView(
    int Id,
    DateOnly Date,
    string Titre,
    string? Moyens,
    AtteinteObjectifCoaching Atteinte);

public sealed record PeriodeObjectifsCoachingJeuneView(
    int Id,
    DateOnly DateDebut,
    DateOnly DateFin,
    IReadOnlyList<ObjectifCoachingJeuneView> Objectifs);
