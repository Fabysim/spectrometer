using Spectrometre.Modules.JeunesPrestataires.Entities;
using Spectrometre.Modules.Missions.Catalog;

namespace Spectrometre.Modules.Missions.Catalog;

/// <summary>Dernière évaluation particulier — drapeaux nuls = non renseignés (ignorés).</summary>
public sealed record MesAstucesEvalSignaux(
    bool? Ponctualite,
    bool? ConsignesComprises,
    bool? TacheRealiseeCorrectement,
    bool? AttitudeRespectueuse);

/// <summary>
/// Correspondance signal → fiches <see cref="MesAstucesCatalog"/>, calculée à la volée
/// (même esprit que <see cref="BadgeCatalog"/> — pas de table de recommandations).
/// </summary>
public static class MesAstucesRecommandationsCatalog
{
    /// <summary>Plafond d'affichage : assez pour un axe de travail, trop peu pour noyer.</summary>
    public const int MaxFiches = 3;

    /// <summary>
    /// Jeune sans mission terminée, <see cref="ProfilAccompagnement.SansExperience"/> :
    /// politesse, présentation, ponctualité — premiers gestes avant d'avoir un retour terrain.
    /// </summary>
    public static readonly string[] StarterSansExperience =
        ["dire_bonjour", "se_presenter", "arriver_a_lheure"];

    /// <summary>
    /// Jeune sans mission terminée, <see cref="ProfilAccompagnement.Autonome"/> :
    /// déjà un peu d'expérience — présentation et prévention plutôt que « dire bonjour ».
    /// </summary>
    public static readonly string[] StarterAutonome =
        ["se_presenter", "prevenir_probleme"];

    /// <summary>
    /// Priorité : évaluation particulière (retour de mission le plus récent), puis grille,
    /// puis starter si aucune mission terminée. Dédoublonnage, puis troncature à <see cref="MaxFiches"/>.
    /// </summary>
    public static IReadOnlyList<MesAstucesFicheDef> Selectionner(
        bool aucuneMissionTerminee,
        ProfilAccompagnement profil,
        MesAstucesEvalSignaux? derniereEval,
        int? dernierScoreCommunication,
        int? dernierScoreAutonomie)
    {
        var keys = new List<string>(MaxFiches + 2);

        if (aucuneMissionTerminee)
        {
            Ajouter(keys, profil == ProfilAccompagnement.Autonome ? StarterAutonome : StarterSansExperience);
            return VersFiches(keys);
        }

        if (derniereEval is not null)
        {
            if (derniereEval.Ponctualite == false)
                Ajouter(keys, "arriver_a_lheure", "en_retard");
            if (derniereEval.ConsignesComprises == false)
                Ajouter(keys, "demander_aide");
            if (derniereEval.TacheRealiseeCorrectement == false)
                Ajouter(keys, "finir_mission");
            // Extra : même source que BadgeCriterion.EvalAttitudeRespectueuse.
            if (derniereEval.AttitudeRespectueuse == false)
                Ajouter(keys, "dire_bonjour");
        }

        if (dernierScoreCommunication is int comm && comm < BadgeCatalog.GrilleScoreSeuil)
            Ajouter(keys, "se_presenter", "demander_aide");
        // Extra : autonomie basse → demander de l'aide / clore proprement (même seuil que les badges grille).
        if (dernierScoreAutonomie is int auto && auto < BadgeCatalog.GrilleScoreSeuil)
            Ajouter(keys, "demander_aide", "finir_mission");

        return VersFiches(keys);
    }

    private static void Ajouter(List<string> keys, params string[] ajouts)
    {
        foreach (var key in ajouts)
        {
            if (keys.Count >= MaxFiches)
                return;
            if (!keys.Contains(key, StringComparer.Ordinal))
                keys.Add(key);
        }
    }

    private static IReadOnlyList<MesAstucesFicheDef> VersFiches(List<string> keys) =>
        keys.Select(k => MesAstucesCatalog.Fiches.First(f => f.Key == k)).ToList();
}
