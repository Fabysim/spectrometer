using Spectrometre.Modules.GestionDuTemps.Entities;
using Spectrometre.Modules.GestionDuTemps.Services;

namespace Spectrometre.Modules.Coaching.Services;

/// <summary>
/// Construction du prompt et texte de repli pour l'anamnèse coach — mêmes principes que
/// <c>SyntheseNarrativeBuilder</c> (GestionDuTemps) : jamais de sortie JSON structurée ici (l'anamnèse est un
/// texte libre affiché tel quel, pas une donnée consommée en aval), et un repli algorithmique simple si l'IA
/// est indisponible, jamais une erreur brute affichée au coach.
/// </summary>
internal static class AnamneseNarrativeBuilder
{
    public static string BuildSystemPrompt(bool english)
    {
        const string basePrompt = """
            Tu es un analyste qui aide un coach professionnel à rédiger une anamnèse (note d'entrée en
            accompagnement) à partir des données de gestion du temps d'une personne qu'il accompagne. Rédige
            un texte de 3 à 5 phrases, factuel et bienveillant, jamais alarmiste, qui aide le coach à préparer
            sa première séance. Réponds UNIQUEMENT avec le texte de l'anamnèse, sans titre, sans markdown.
            """;

        return english
            ? basePrompt + "\n\nIMPORTANT: Write the anamnesis in English."
            : basePrompt;
    }

    public static string BuildUserPrompt(ProfilPsychosocial? profil, ReflexionConsciente? reflexion, SyntheseView? synthese)
    {
        if (profil is null)
            return "Cette personne n'a pas encore rempli son profil psychosocial ni sa réflexion consciente. Rédige une anamnèse très courte indiquant qu'aucune donnée n'est encore disponible.";

        return $"""
            ## PROFIL PSYCHOSOCIAL
            - Sommeil réparateur : {Val(profil.SommeilReparateur)}
            - Sentiment de pression : {Val(profil.SentimentPression)}
            - Interruptions au travail : {Val(profil.InterruptionsTravail)}
            - Tolérance à l'imprévu : {Val(profil.ToleranceImprevu)}
            - Planification à l'avance : {Val(profil.PlanificationAvance)}
            - Déséquilibres identifiés : {JoinOrNone(profil.Desequilibres)}
            - Émotions négatives : {JoinOrNone(profil.EmotionsNegatives)}
            - Objectifs professionnels : {JoinOrNone(profil.ObjectifsProfessionnels)}

            ## RÉFLEXION CONSCIENTE DU MOMENT
            - Situation actuelle : {Val(reflexion?.SituationActuelle)}
            - Ressentis : {JoinOrNone(reflexion?.Ressentis ?? [])}

            ## SYNTHÈSE DU CYCLE (calcul déterministe)
            - Profil type : {Val(synthese?.ProfilType)}
            - Indice d'équilibre : {(synthese is null ? "non renseigné" : synthese.IndiceEquilibre + "/100")}
            - Niveau de maturité organisationnelle : {(synthese is null ? "non renseigné" : synthese.NiveauMaturite + "/5")}

            Rédige l'anamnèse pour le coach à partir de ces éléments.
            """;
    }

    private static string Val(string? v) => string.IsNullOrWhiteSpace(v) ? "non renseigné" : v;

    private static string JoinOrNone(IEnumerable<string> items)
    {
        var list = items.ToList();
        return list.Count == 0 ? "aucun(e)" : string.Join(", ", list);
    }

    public static string BuildFallback(ProfilPsychosocial? profil, SyntheseView? synthese, bool english)
    {
        if (profil is null)
        {
            return english
                ? "This person has not yet completed their psychosocial profile or conscious reflection. No data is available yet to prepare this anamnesis."
                : "Cette personne n'a pas encore rempli son profil psychosocial ni sa réflexion consciente. Aucune donnée n'est encore disponible pour préparer cette anamnèse.";
        }

        var profilType = synthese?.ProfilType ?? "Réactif";
        return english
            ? $"Based on the available data, this person's dominant profile is \"{profilType}\" (see the full synthesis for details). Review the psychosocial profile and conscious reflection together with them at the first session to refine this reading."
            : $"D'après les données disponibles, le profil dominant de cette personne est « {profilType} » (voir la synthèse complète pour le détail). Reprenez le profil psychosocial et la réflexion consciente avec elle en première séance pour affiner cette lecture.";
    }
}
