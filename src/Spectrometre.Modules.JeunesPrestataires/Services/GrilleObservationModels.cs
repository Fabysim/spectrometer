namespace Spectrometre.Modules.JeunesPrestataires.Services;

public enum GrilleObservationAccessMode
{
    None,
    Jeune,
    Coach,
}

public sealed record GrilleObservationCritereInput(string CritereKey, int? Score, string? Commentaire);

public sealed record GrilleObservationCritereView(string CritereKey, int? Score, string? Commentaire);

public sealed record GrilleObservationHistoriqueItemView(
    int EvaluationId,
    DateTimeOffset EvalueeLe,
    double? MoyenneScore);

/// <summary>
/// Détail d'une évaluation — <see cref="CommentaireGeneral"/> est <c>null</c> pour un accès jeune
/// (filtrage côté service, jamais UI seule).
/// </summary>
public sealed record GrilleObservationEvaluationDetailView(
    int EvaluationId,
    DateTimeOffset EvalueeLe,
    string? CommentaireGeneral,
    IReadOnlyList<GrilleObservationCritereView> Criteres,
    GrilleObservationAccessMode AccessMode,
    JeuneProfileView JeuneProfile);

public sealed record GrilleObservationPageView(
    GrilleObservationAccessMode AccessMode,
    JeuneProfileView JeuneProfile,
    IReadOnlyList<GrilleObservationHistoriqueItemView> Historique,
    bool CanCreateEvaluation);
