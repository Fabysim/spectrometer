namespace Spectrometre.Modules.Missions.Catalog;

/// <summary>
/// Source de calcul d'un badge — toujours dérivée de données déjà persistées, jamais d'une table d'obtention.
/// </summary>
public enum BadgeCriterion
{
    MissionsTerminees,
    EvalPonctualite,
    EvalTacheRealisee,
    EvalNouvelleMission,
    EvalAttitudeRespectueuse,
    GrilleScore,
    CharteAcceptee,
}

/// <summary>Définition statique (clé + critère + seuil). Libellés via <c>Badge_{Key}_Titre</c> / <c>_Description</c>.</summary>
public sealed record BadgeDef(
    string Key,
    string Emoji,
    BadgeCriterion Criterion,
    int Threshold,
    string? GrilleCritereKey = null);

/// <summary>
/// Badges calculés à la volée à chaque affichage — pas de table <c>BadgeObtenu</c> dans ce cycle
/// (évite le recalcul rétroactif et les badges qui se « dé-débloquent » de façon désynchronisée).
/// Une notification « nouveau badge » pourrait s'appuyer plus tard sur un historique stocké ; ce n'est pas demandé ici.
/// </summary>
public static class BadgeCatalog
{
    /// <summary>
    /// Échelle grille : <see cref="Spectrometre.Modules.JeunesPrestataires.Entities.GrilleObservationCritere.Score"/>
    /// est 1–5 (null si non renseigné). Seuil 4 = « plutôt oui / tout à fait » sur cette échelle,
    /// au-dessus de la moyenne 3, cohérent avec le langage 1–5 de l'auto-observation.
    /// Débloqué dès qu'au moins une évaluation atteint ce seuil (date = première fois).
    /// </summary>
    public const int GrilleScoreSeuil = 4;

    /// <summary>Paliers Bouchra « Ponctuel 5 fois » : 1, 5 et 10 évaluations particulières positives.</summary>
    public static readonly int[] PaliersEvaluation = [1, 5, 10];

    /// <summary>« Toujours partant » / « Respectueux » : 3 retours positifs — assez pour une récurrence, sans attendre 10 missions.</summary>
    public const int SeuilRecurrence = 3;

    public static IReadOnlyList<BadgeDef> All { get; } =
    [
        new("premiere_mission", "🎯", BadgeCriterion.MissionsTerminees, 1),
        new("ponctuel_1", "⏰", BadgeCriterion.EvalPonctualite, 1),
        new("ponctuel_5", "⏰", BadgeCriterion.EvalPonctualite, 5),
        new("ponctuel_10", "⏰", BadgeCriterion.EvalPonctualite, 10),
        new("mission_reussie_1", "✅", BadgeCriterion.EvalTacheRealisee, 1),
        new("mission_reussie_5", "✅", BadgeCriterion.EvalTacheRealisee, 5),
        new("mission_reussie_10", "✅", BadgeCriterion.EvalTacheRealisee, 10),
        new("toujours_partant", "🙌", BadgeCriterion.EvalNouvelleMission, SeuilRecurrence),
        new("autonome", "🧭", BadgeCriterion.GrilleScore, GrilleScoreSeuil, "autonomie"),
        new("bon_communicant", "💬", BadgeCriterion.GrilleScore, GrilleScoreSeuil, "communication"),
        new("esprit_initiative", "💡", BadgeCriterion.GrilleScore, GrilleScoreSeuil, "initiative"),
        new("charte", "📜", BadgeCriterion.CharteAcceptee, 1),
        // Extra — données déjà collectées, même esprit que les exemples Bouchra.
        new("respectueux", "🤝", BadgeCriterion.EvalAttitudeRespectueuse, SeuilRecurrence),
        new("soin_travail", "✨", BadgeCriterion.GrilleScore, GrilleScoreSeuil, "soin_travail"),
    ];
}
