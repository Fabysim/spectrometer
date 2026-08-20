using System.Text;
using Spectrometre.Modules.JeunesPrestataires.Catalog;

namespace Spectrometre.Modules.JeunesPrestataires.Services;

/// <summary>
/// Génération section 13 par règles — pas d'IA v1. Une synthèse IA pourrait mieux relier
/// les nuances des champs libres ; à valider métier avant implémentation.
/// </summary>
public static class AutoObservationSyntheseGenerator
{
    public static string Generer(IReadOnlyDictionary<string, AutoObservationAnswerView> answers)
    {
        var sb = new StringBuilder();

        AppendForces(sb, answers);
        AppendCompetences(sb, answers);
        AppendEncadrement(sb, answers);
        AppendPreferences(sb, answers);
        AppendLimites(sb, answers);
        AppendObjectifs(sb, answers);

        return sb.ToString().Trim();
    }

    private static void AppendForces(StringBuilder sb, IReadOnlyDictionary<string, AutoObservationAnswerView> a)
    {
        sb.AppendLine("## Forces perçues");
        var qualites = GetMulti(a, "p2.s3.qualites");
        if (qualites.Count > 0)
            sb.AppendLine("- Qualités reconnues : " + string.Join(", ", qualites));
        var autresDisent = GetText(a, "p2.s3.autres_disent");
        if (!string.IsNullOrWhiteSpace(autresDisent))
            sb.AppendLine("- Ce que les autres disent : " + autresDisent);
        var utile = GetText(a, "p2.s3.utile");
        if (!string.IsNullOrWhiteSpace(utile))
            sb.AppendLine("- Situations d'utilité : " + utile);
        var energie = GetText(a, "p1.s5.energie");
        if (!string.IsNullOrWhiteSpace(energie))
            sb.AppendLine("- Énergie (synthèse partie 1) : " + energie);
        if (sb.ToString().EndsWith("Forces perçues\n", StringComparison.Ordinal))
            sb.AppendLine("- (Données insuffisantes — compléter sections 1 et 3)");
        sb.AppendLine();
    }

    private static void AppendCompetences(StringBuilder sb, IReadOnlyDictionary<string, AutoObservationAnswerView> a)
    {
        sb.AppendLine("## Compétences mobilisables");
        var taches = GetMulti(a, "p2.s2.taches");
        if (taches.Count > 0)
            sb.AppendLine("- Tâches déjà réalisées : " + string.Join(", ", taches));
        var capable = GetText(a, "p2.s2.capable");
        if (!string.IsNullOrWhiteSpace(capable))
            sb.AppendLine("- Capable aujourd'hui : " + capable);
        var outils = GetText(a, "p2.s2.outils");
        if (!string.IsNullOrWhiteSpace(outils))
            sb.AppendLine("- Outils / matériels : " + outils);
        var experiences = GetMulti(a, "p2.s2.experience");
        if (experiences.Count > 0)
            sb.AppendLine("- Expériences utiles : " + string.Join(", ", experiences));
        sb.AppendLine();
    }

    private static void AppendEncadrement(StringBuilder sb, IReadOnlyDictionary<string, AutoObservationAnswerView> a)
    {
        sb.AppendLine("## Besoins d'encadrement");
        var faibles = new List<string>();
        foreach (var (key, label) in ScaleKeysEncadrement)
        {
            var score = GetScale(a, key);
            if (score is <= 2)
                faibles.Add($"{label} ({score}/5)");
        }
        foreach (var (key, label) in Scale4KeysEncadrement)
        {
            var score = GetScale(a, key);
            if (score is <= 2)
                faibles.Add($"{label} ({score}/4)");
        }
        var aideOrg = GetMulti(a, "p2.s8.aide_org");
        if (faibles.Count > 0)
            sb.AppendLine("- Scores faibles : " + string.Join("; ", faibles));
        if (aideOrg.Count > 0)
            sb.AppendLine("- Aides organisation souhaitées : " + string.Join(", ", aideOrg));
        var coachAide = GetText(a, "p2.s12.aide_coach");
        if (!string.IsNullOrWhiteSpace(coachAide))
            sb.AppendLine("- Attentes vis-à-vis du coach : " + coachAide);
        if (faibles.Count == 0 && aideOrg.Count == 0 && string.IsNullOrWhiteSpace(coachAide))
            sb.AppendLine("- Peu de signaux faibles — vérifier sections communication et organisation.");
        sb.AppendLine();
    }

    private static void AppendPreferences(StringBuilder sb, IReadOnlyDictionary<string, AutoObservationAnswerView> a)
    {
        sb.AppendLine("## Préférences de missions");
        var missions = GetMulti(a, "p2.s12.missions_priorite");
        if (missions.Count > 0)
            sb.AppendLine("- Missions prioritaires : " + string.Join(", ", missions));
        var privilegier = GetText(a, "p1.s5.missions_privilegier");
        if (!string.IsNullOrWhiteSpace(privilegier))
            sb.AppendLine("- À privilégier (synthèse) : " + privilegier);
        var contextes = GetMulti(a, "p2.s12.contextes");
        if (contextes.Count > 0)
            sb.AppendLine("- Contextes préférés : " + string.Join(", ", contextes));
        var jours = GetMulti(a, "p2.s11.jours");
        var plages = GetMulti(a, "p2.s11.plages");
        if (jours.Count > 0 || plages.Count > 0)
            sb.AppendLine("- Disponibilités : " + string.Join(", ", jours.Concat(plages)));
        var distance = GetText(a, "p2.s11.distance");
        if (!string.IsNullOrWhiteSpace(distance))
            sb.AppendLine("- Distance max : " + distance);
        sb.AppendLine();
    }

    private static void AppendLimites(StringBuilder sb, IReadOnlyDictionary<string, AutoObservationAnswerView> a)
    {
        sb.AppendLine("## Limites actuelles");
        var refus = GetText(a, "p2.s12.missions_refus");
        if (!string.IsNullOrWhiteSpace(refus))
            sb.AppendLine("- Missions refusées : " + refus);
        var eviter = GetText(a, "p1.s5.missions_eviter");
        if (!string.IsNullOrWhiteSpace(eviter))
            sb.AppendLine("- À éviter ou préparer : " + eviter);
        var situations = GetMulti(a, "p2.s12.situations_difficulte");
        if (situations.Count > 0)
            sb.AppendLine("- Situations difficiles : " + string.Join(", ", situations));
        var contraintes = GetMulti(a, "p2.s11.contraintes");
        if (contraintes.Count > 0)
            sb.AppendLine("- Contraintes : " + string.Join(", ", contraintes));
        sb.AppendLine();
    }

    private static void AppendObjectifs(StringBuilder sb, IReadOnlyDictionary<string, AutoObservationAnswerView> a)
    {
        sb.AppendLine("## Objectifs prioritaires de progression");
        var objectifs = new List<string>();
        foreach (var (key, label) in ObjectifScaleKeys)
        {
            var score = GetScale(a, key);
            if (score is <= 2)
                objectifs.Add(label);
        }
        var renforcer = GetText(a, "p2.s3.renforcer");
        if (!string.IsNullOrWhiteSpace(renforcer))
            objectifs.Add("Renforcer : " + renforcer);
        var progression = GetText(a, "p1.s5.points_progression");
        if (!string.IsNullOrWhiteSpace(progression))
            objectifs.Add("Points synthèse : " + progression);
        if (objectifs.Count > 0)
        {
            var top = objectifs.Take(3);
            foreach (var o in top)
                sb.AppendLine("- " + o);
        }
        else
            sb.AppendLine("- Définir 2 ou 3 objectifs avec le coach (ponctualité, communication, soin, autonomie, présentation).");
    }

    private static readonly (string Key, string Label)[] ScaleKeysEncadrement =
    [
        ("p2.s5.poser_question", "Oser poser une question"),
        ("p2.s6.presence_rassurante", "Besoin présence rassurante"),
        ("p2.s8.preparer", "Préparation mission"),
        ("p2.s8.ponctualite", "Ponctualité"),
        ("p2.s8.etapes", "Organisation multi-étapes"),
    ];

    private static readonly (string Key, string Label)[] Scale4KeysEncadrement =
    [
        ("p2.s5.reformuler", "Reformuler consignes"),
        ("p2.s5.prevenir", "Prévenir en cas de problème"),
    ];

    private static readonly (string Key, string Label)[] ObjectifScaleKeys =
    [
        ("p2.s8.ponctualite", "Ponctualité"),
        ("p2.s5.poser_question", "Communication / questions"),
        ("p2.s7.verifier", "Soin / vérification résultat"),
        ("p2.s8.etapes", "Autonomie organisation"),
        ("p2.s9.presenter", "Présentation de soi"),
        ("p2.s3.progresser", "Désir de progresser"),
    ];

    private static string? GetText(IReadOnlyDictionary<string, AutoObservationAnswerView> a, string key) =>
        a.TryGetValue(key, out var v) ? v.TextValue : null;

    private static int? GetScale(IReadOnlyDictionary<string, AutoObservationAnswerView> a, string key) =>
        a.TryGetValue(key, out var v) ? v.NumericValue : null;

    private static List<string> GetMulti(IReadOnlyDictionary<string, AutoObservationAnswerView> a, string key)
    {
        if (!a.TryGetValue(key, out var v) || string.IsNullOrWhiteSpace(v.TextValue))
            return [];
        return v.TextValue.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }
}
