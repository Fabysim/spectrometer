using Spectrometre.Modules.JeunesPrestataires.Entities;

namespace Spectrometre.Modules.JeunesPrestataires.Services;

/// <summary>
/// Tableaux de synthèse par règles (pas d'IA) — document Bouchra « Révision des tableaux de synthèse ».
/// Tableau 3 (plan d'action coach) hors scope.
/// </summary>
public static class AutoObservationSyntheseGenerator
{
    public const string DonneesInsuffisantes = "À compléter avec le coach.";

    public const string TitreTroncCommun =
        "Tableau 1 — Tronc commun (tous les jeunes)";

    public const string TitreEmployabilite =
        "Tableau 2A — Développement de l'employabilité";

    public const string TitreOrientation =
        "Tableau 2B — Orientation et projet professionnel";

    public static AutoObservationSyntheseDocument Generer(
        IReadOnlyDictionary<string, AutoObservationAnswerView> answers,
        ProfilAccompagnement profil,
        IReadOnlyList<(string CritereKey, int? Score)>? grilleDerniere = null)
    {
        var tronc = new AutoObservationSyntheseTableau(
            "T1",
            TitreTroncCommun,
            [
                Ligne("Forces et qualités perçues", Forces(answers)),
                Ligne("Besoins d'encadrement / niveau de soutien requis", Encadrement(answers, grilleDerniere)),
                Ligne("Contextes favorables / conditions de réussite", ContextesFavorables(answers)),
                Ligne("Situations stressantes / points de vigilance", Vigilance(answers, grilleDerniere)),
                Ligne("Objectifs de progression / axes de développement", Objectifs(answers)),
            ]);

        AutoObservationSyntheseTableau? t2a = null;
        AutoObservationSyntheseTableau? t2b = null;
        if (profil == ProfilAccompagnement.Autonome)
        {
            t2b = new AutoObservationSyntheseTableau(
                "T2B",
                TitreOrientation,
                [
                    Ligne("Expériences significatives / sources d'information sur le profil", Experiences(answers)),
                    Ligne("Activités énergisantes / intérêts dominants", Energisantes(answers)),
                    Ligne("Activités épuisantes / situations à éviter", Epuisantes(answers)),
                    Ligne("Valeurs prioritaires / critères de choix", Valeurs(answers)),
                    Ligne("Conditions de réussite / environnement compatible", ConditionsReussite(answers)),
                    Ligne("Pistes d'études ou métiers envisagés / hypothèses à explorer", Pistes(answers)),
                ]);
        }
        else
        {
            t2a = new AutoObservationSyntheseTableau(
                "T2A",
                TitreEmployabilite,
                [
                    Ligne("Missions que le jeune souhaite essayer / premières missions à proposer", MissionsAEssayer(answers)),
                    Ligne("Missions à éviter ou à préparer / accompagnement nécessaire", MissionsAEviter(answers)),
                    Ligne("Motivations principales / leviers d'engagement", Motivations(answers)),
                    Ligne("Habitudes de travail déjà présentes / autonomie actuelle", HabitudesPresentes(answers)),
                    Ligne("Habitudes à développer / objectifs prioritaires", HabitudesADevelopper(answers)),
                ]);
        }

        return new AutoObservationSyntheseDocument(
            AutoObservationSyntheseDocument.VersionCourante,
            profil.ToString(),
            tronc,
            t2a,
            t2b);
    }

    private static AutoObservationSyntheseLigne Ligne(string theme, string contenu) =>
        new(theme, string.IsNullOrWhiteSpace(contenu) ? DonneesInsuffisantes : contenu);

    private static string Forces(IReadOnlyDictionary<string, AutoObservationAnswerView> a) =>
        Joindre(
            Prefixe("Qualités", GetMulti(a, "p2.s3.qualites")),
            Prefixe("Ce que les autres disent", GetText(a, "p2.s3.autres_disent")),
            Prefixe("Situations d'utilité", GetText(a, "p2.s3.utile")),
            Prefixe("Énergie", GetText(a, "p1.s5.energie")),
            Prefixe("Preuves de compétence", GetMulti(a, "p0.s3.preuves")));

    private static string Encadrement(
        IReadOnlyDictionary<string, AutoObservationAnswerView> a,
        IReadOnlyList<(string CritereKey, int? Score)>? grille)
    {
        var faibles = EchellesFaibles(a, ScaleKeysEncadrement, 5)
            .Concat(EchellesFaibles(a, Scale4KeysEncadrement, 4))
            .Concat(GrilleFaibles(grille))
            .ToList();
        return Joindre(
            Prefixe("Scores à renforcer", faibles),
            Prefixe("Aides d'organisation souhaitées", GetMulti(a, "p2.s8.aide_org")),
            Prefixe("Attentes vis-à-vis du coach", GetText(a, "p2.s12.aide_coach")),
            Prefixe("Niveau d'autonomie souhaité", GetMulti(a, "p0.s2.niveau_autonomie")),
            Prefixe("Forme des consignes", GetMulti(a, "p2.s5.consignes_forme")));
    }

    private static string ContextesFavorables(IReadOnlyDictionary<string, AutoObservationAnswerView> a) =>
        Joindre(
            Prefixe("Contextes préférés", GetMulti(a, "p2.s12.contextes")),
            Prefixe("Contextes qui me conviennent", GetMulti(a, "p0.s4.contextes")),
            Prefixe("Moments d'énergie", GetMulti(a, "p0.s4.moments_energie")),
            Prefixe("Attitudes qui mettent en confiance", GetMulti(a, "p2.s6.attitudes_confiance")),
            Prefixe("Ce qui aide à communiquer", GetText(a, "p2.s5.aide_communiquer")));

    private static string Vigilance(
        IReadOnlyDictionary<string, AutoObservationAnswerView> a,
        IReadOnlyList<(string CritereKey, int? Score)>? grille) =>
        Joindre(
            Prefixe("Quand je suis stressé", GetMulti(a, "p2.s5.stress_tendance")),
            Prefixe("Situations difficiles", GetMulti(a, "p2.s12.situations_difficulte")),
            Prefixe("Moments de fatigue", GetMulti(a, "p0.s4.moments_fatigue")),
            Prefixe("Attitudes qui mettent en difficulté", GetMulti(a, "p2.s6.attitudes_difficulte")),
            Prefixe("Peur d'être observé", GetMulti(a, "p0.s3.peur_observe")),
            Prefixe("Grille d'observation (scores bas)", GrilleFaibles(grille)));

    private static string Objectifs(IReadOnlyDictionary<string, AutoObservationAnswerView> a)
    {
        var axes = EchellesFaibles(a, ObjectifScaleKeys, 5).Take(3).ToList();
        return Joindre(
            Prefixe("Axes (échelles basses)", axes),
            Prefixe("Qualité à renforcer", GetText(a, "p2.s3.renforcer")),
            Prefixe("Points de progression", GetText(a, "p1.s5.points_progression")),
            Prefixe("Besoins de départ", GetMulti(a, "p0.s6.besoins_depart")));
    }

    private static string MissionsAEssayer(IReadOnlyDictionary<string, AutoObservationAnswerView> a) =>
        Joindre(
            Prefixe("Missions prioritaires", GetMulti(a, "p2.s12.missions_priorite")),
            Prefixe("Tâches envie d'apprendre", GetText(a, "p2.s2.apprendre")),
            Prefixe("À privilégier", GetText(a, "p1.s5.missions_privilegier")));

    private static string MissionsAEviter(IReadOnlyDictionary<string, AutoObservationAnswerView> a) =>
        Joindre(
            Prefixe("Missions refusées", GetText(a, "p2.s12.missions_refus")),
            Prefixe("À éviter ou préparer", GetText(a, "p1.s5.missions_eviter")));

    private static string Motivations(IReadOnlyDictionary<string, AutoObservationAnswerView> a) =>
        Joindre(
            Prefixe("Motivation d'inscription", GetMulti(a, "p2.s1.motivation")),
            Prefixe("Leviers d'engagement", GetMulti(a, "p2.s10.motive")),
            Prefixe("Ce qui aide à rester engagé", GetText(a, "p2.s10.engage")));

    private static string HabitudesPresentes(IReadOnlyDictionary<string, AutoObservationAnswerView> a)
    {
        var fortes = new List<string>();
        foreach (var (key, label) in ObjectifScaleKeys)
        {
            var score = GetScale(a, key);
            if (score is >= 4)
                fortes.Add($"{label} ({score}/5)");
        }

        return Joindre(
            Prefixe("Tâches déjà réalisées", GetMulti(a, "p2.s2.taches")),
            Prefixe("Capable aujourd'hui", GetText(a, "p2.s2.capable")),
            Prefixe("Autonomie", GetMulti(a, "p0.s2.niveau_autonomie")),
            Prefixe("Habitudes solides", fortes));
    }

    private static string HabitudesADevelopper(IReadOnlyDictionary<string, AutoObservationAnswerView> a) =>
        Joindre(
            Prefixe("Objectifs", EchellesFaibles(a, ObjectifScaleKeys, 5)),
            Prefixe("Tâches à progresser", GetText(a, "p2.s7.progresser")),
            Prefixe("Présentation à améliorer", GetText(a, "p2.s9.ameliorer")));

    private static string Experiences(IReadOnlyDictionary<string, AutoObservationAnswerView> a) =>
        Joindre(
            Prefixe("Expériences marquantes", GetMulti(a, "p0.s1.experiences")),
            Prefixe("Expériences utiles", GetMulti(a, "p2.s2.experience")),
            Prefixe("Ce que j'en retiens", GetText(a, "p2.s2.experience_appris")));

    private static string Energisantes(IReadOnlyDictionary<string, AutoObservationAnswerView> a) =>
        Joindre(
            Prefixe("Moments d'énergie", GetMulti(a, "p0.s4.moments_energie")),
            Prefixe("Activité idéale", GetMulti(a, "p0.s4.activite_ideale")),
            Prefixe("Types de travail", GetMulti(a, "p2.s4.types")),
            Prefixe("Exemple", GetText(a, "p2.s4.exemple_activite")));

    private static string Epuisantes(IReadOnlyDictionary<string, AutoObservationAnswerView> a) =>
        Joindre(
            Prefixe("Moments de fatigue", GetMulti(a, "p0.s4.moments_fatigue")),
            Prefixe("Ce qui me manque", GetMulti(a, "p0.s4.manque")),
            Prefixe("Missions à éviter", GetText(a, "p1.s5.missions_eviter")));

    private static string Valeurs(IReadOnlyDictionary<string, AutoObservationAnswerView> a) =>
        Joindre(
            Prefixe("Valeurs (exploration)", GetMulti(a, "p0.s5.valeurs")),
            Prefixe("Valeurs importantes", GetMulti(a, "p2.s10.valeurs")));

    private static string ConditionsReussite(IReadOnlyDictionary<string, AutoObservationAnswerView> a) =>
        Joindre(
            Prefixe("Contextes", GetMulti(a, "p0.s4.contextes")),
            Prefixe("Contextes de mission", GetMulti(a, "p2.s12.contextes")),
            Prefixe("Besoins de départ", GetMulti(a, "p0.s6.besoins_depart")));

    private static string Pistes(IReadOnlyDictionary<string, AutoObservationAnswerView> a) =>
        Joindre(
            Prefixe("Pistes envisagées", GetMulti(a, "p0.s5.pistes")),
            Prefixe("Piste prioritaire", GetText(a, "p0.s8.piste_prioritaire")),
            Prefixe("Raison du choix", GetText(a, "p0.s8.raison_choix")),
            Prefixe("Piste à tester", GetText(a, "p0.s5.piste_tester")));

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

    private static readonly Dictionary<string, string> GrilleLibelles = new(StringComparer.Ordinal)
    {
        ["ponctualite"] = "ponctualité",
        ["comprehension_consignes"] = "compréhension des consignes",
        ["autonomie"] = "autonomie",
        ["communication"] = "communication",
        ["soin_travail"] = "soin du travail",
        ["respect_cadre"] = "respect du cadre",
        ["initiative"] = "initiative",
        ["sens_responsabilite"] = "sens des responsabilités",
        ["sens_engagement"] = "sens de l'engagement",
    };

    private static List<string> EchellesFaibles(
        IReadOnlyDictionary<string, AutoObservationAnswerView> a,
        (string Key, string Label)[] keys,
        int max)
    {
        var list = new List<string>();
        foreach (var (key, label) in keys)
        {
            var score = GetScale(a, key);
            if (score is <= 2)
                list.Add($"{label} ({score}/{max})");
        }

        return list;
    }

    private static List<string> GrilleFaibles(IReadOnlyList<(string CritereKey, int? Score)>? grille)
    {
        if (grille is null || grille.Count == 0)
            return [];

        var list = new List<string>();
        foreach (var (key, score) in grille)
        {
            if (score is not <= 2)
                continue;
            var label = GrilleLibelles.TryGetValue(key, out var lib) ? lib : key;
            list.Add($"{label} ({score}/5)");
        }

        return list;
    }

    private static string Joindre(params string?[] parts)
    {
        var items = parts.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        return items.Count == 0 ? "" : string.Join(" · ", items);
    }

    private static string? Prefixe(string prefixe, string? valeur) =>
        string.IsNullOrWhiteSpace(valeur) ? null : $"{prefixe} : {valeur}";

    private static string? Prefixe(string prefixe, IReadOnlyList<string> valeurs) =>
        valeurs.Count == 0 ? null : $"{prefixe} : {string.Join(", ", valeurs)}";

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
