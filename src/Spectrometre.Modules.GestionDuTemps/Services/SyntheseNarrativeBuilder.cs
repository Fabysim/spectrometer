using System.Text.Json;
using Spectrometre.Modules.GestionDuTemps.Entities;

namespace Spectrometre.Modules.GestionDuTemps.Services;

public sealed record RecommandationIa(int Priorite, string Texte, string Domaine);

/// <summary>Partie narrative de la synthèse — les nombres (<c>ProfilType</c> mis à part) viennent toujours de <see cref="SyntheseCalculator"/>, jamais de l'IA. Voir <see cref="Entities.Synthese"/>.</summary>
public sealed record SyntheseContenuNarratif(
    string ProfilType,
    string? ProfilTexte,
    string? IndiceCommentaire,
    string? MaturiteCommentaire,
    IReadOnlyList<RecommandationIa> Recommandations,
    IReadOnlyList<string> Alertes,
    bool GenereeParIa);

/// <summary>
/// Résumé de l'activité du cycle actif (comptes Kanban + temps réel cumulé) — construit exclusivement à
/// partir des données du module Gestion du temps (<see cref="Activite"/>/<see cref="KanbanStatut"/>),
/// jamais d'un autre module, pour enrichir le prompt IA sans se limiter au seul questionnaire déclaratif.
/// </summary>
public sealed record ResumeActiviteCycle(int TotalActivites, int Terminees, int EnCours, int AFaire, long TempsReelTotalMs);

/// <summary>
/// Construction du prompt et interprétation de la réponse IA pour la synthèse — repris de
/// <c>GdtSyntheseIaService</c> (mvp), sans le support multilingue (cette application reste en français).
/// Aucune dépendance à un autre module : uniquement <see cref="ProfilPsychosocial"/>,
/// <see cref="ReflexionConsciente"/> et <see cref="ResumeActiviteCycle"/> (données du module lui-même).
/// </summary>
internal static class SyntheseNarrativeBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static string BuildSystemPrompt() =>
        """
        Tu es un analyste en gestion du temps et en équilibre de vie. On te fournit le profil psychosocial
        d'une personne, sa réflexion consciente du moment et un résumé de son activité récente. Réponds
        UNIQUEMENT en JSON valide, sans markdown, sans texte avant ou après. Sois concret, bienveillant et
        directement utile — jamais générique ni déconnecté des données fournies.
        """;

    public static string BuildUserPrompt(ProfilPsychosocial p, ReflexionConsciente? reflexion, ResumeActiviteCycle resume)
    {
        const string jsonSchema = """
            {
              "profilType": "Saturé|Fragmenté|Réactif|Structuré|Anticipatif",
              "profilTexte": "2-3 phrases reliant les éléments du profil de façon personnalisée",
              "indiceCommentaire": "1 phrase contextualisant l'équilibre global",
              "maturiteCommentaire": "1 phrase contextualisant le niveau d'organisation",
              "recommandations": [
                { "priorite": 1, "texte": "...", "domaine": "sommeil|perso|pro|admin|famille|social|organisation" }
              ],
              "alertes": ["..."]
            }
            """;

        return $"""
            Analyse ce profil et génère une synthèse JSON. Le champ profilType doit rester une des valeurs
            canoniques listées dans le schéma.

            ## RYTHME DE VIE
            - Sommeil : coucher {Val(p.SommeilCoucher?.ToString("HH:mm"))}, lever {Val(p.SommeilLever?.ToString("HH:mm"))}
            - Sommeil réparateur : {Val(p.SommeilReparateur)}
            - Réveils nocturnes : {BoolLabel(p.ReveilsNocturnes)}
            - Écrans avant sommeil : {BoolLabel(p.EcransAvantSommeil)}
            - Moment personnel quotidien : {BoolLabel(p.TempsPersoQuotidien)}
            - Sentiment de manque de temps perso : {Val(p.ManqueTempsPerso)}
            - Horaire de travail : {Val(p.HoraireTravail)}
            - Travail hors heures prévues : {Val(p.TravailHorsHeures)}

            ## CONTRAINTES
            - Durée des trajets : {Val(p.DureeTrajets)}
            - Déplacements imprévisibles : {BoolLabel(p.DeplacementsImprevus)}
            - Mode de travail : {Val(p.ModeTravail)}
            - Interruptions : {Val(p.InterruptionsTravail)}
            - Autonomie dans la gestion du temps : {Val(p.AutonomieGestion?.ToString())}/5

            ## ÉTAT PSYCHOSOCIAL
            - Sentiment de pression : {Val(p.SentimentPression)}
            - Difficultés de concentration : {Val(p.DifficultesConcentration)}
            - Décisions précipitées : {Val(p.DecisionsPrecipitees)}
            - Tendances : {JoinOrNone(p.Tendances)}
            - Culpabilité temps perso : {Val(p.Culpabilite)}
            - Tolérance à l'imprévu : {Val(p.ToleranceImprevu)}

            ## HABITUDES ORGANISATIONNELLES
            - Utilise un agenda : {BoolLabel(p.UtiliseAgenda)}
            - Planification à l'avance : {Val(p.PlanificationAvance)}
            - Rituels quotidiens : {BoolLabel(p.RituelsQuotidiens)}
            - Dit oui trop facilement : {Val(p.OuiTropFacile)}
            - Capacité à refuser : {Val(p.CapaciteNon)}
            - Gestion administrative : {Val(p.GestionAdmin)}
            - Stress administratif : {Val(p.StressAdmin)}

            ## ÉQUILIBRE DES TEMPS (satisfaction 0=très insatisfait à 4=très satisfait)
            - Sommeil : {Val(p.SatisfactionSommeil?.ToString())}/4, Personnel : {Val(p.SatisfactionPerso?.ToString())}/4, Professionnel : {Val(p.SatisfactionPro?.ToString())}/4
            - Administratif : {Val(p.SatisfactionAdmin?.ToString())}/4, Familial : {Val(p.SatisfactionFamille?.ToString())}/4, Social : {Val(p.SatisfactionSocial?.ToString())}/4
            - Déséquilibres identifiés : {JoinOrNone(p.Desequilibres)}
            - Émotions négatives : {JoinOrNone(p.EmotionsNegatives)}
            - Émotions positives : {JoinOrNone(p.EmotionsPositives)}

            ## OBJECTIFS
            - Objectifs professionnels : {JoinOrNone(p.ObjectifsProfessionnels)}
            - Obstacles perçus : {JoinOrNone(p.Obstacles)}

            ## RÉFLEXION CONSCIENTE DU MOMENT
            - Situation actuelle : {Val(reflexion?.SituationActuelle)}
            - Source identifiée : {BoolLabel(reflexion?.SourceIdentifiee)}
            - Ressentis : {JoinOrNone(reflexion?.Ressentis ?? [])}

            ## ACTIVITÉ DU CYCLE EN COURS
            - {resume.TotalActivites} rappel(s) au total : {resume.Terminees} terminé(s), {resume.EnCours} en cours, {resume.AFaire} à faire
            - Temps réel cumulé enregistré (minuteur) : {resume.TempsReelTotalMs / 60_000} minute(s)

            Génère UNIQUEMENT ce JSON (pas de markdown, pas de texte avant ou après) :
            {jsonSchema}
            Maximum 5 recommandations. Les alertes peuvent être vides [].
            """;
    }

    private static string Val(string? v) => string.IsNullOrWhiteSpace(v) ? "non renseigné" : v;

    private static string BoolLabel(bool? v) => v switch
    {
        true => "oui",
        false => "non",
        null => "non renseigné",
    };

    private static string JoinOrNone(IEnumerable<string> items)
    {
        var list = items.ToList();
        return list.Count == 0 ? "aucun(e)" : string.Join(", ", list);
    }

    public static SyntheseContenuNarratif ParseResponse(string output)
    {
        var clean = output.Replace("```json", "", StringComparison.OrdinalIgnoreCase).Replace("```", "").Trim();
        using var doc = JsonDocument.Parse(clean);
        var root = doc.RootElement;

        var recos = new List<RecommandationIa>();
        if (root.TryGetProperty("recommandations", out var recoEl))
        {
            foreach (var r in recoEl.EnumerateArray())
            {
                recos.Add(new RecommandationIa(
                    r.TryGetProperty("priorite", out var pr) ? pr.GetInt32() : recos.Count + 1,
                    r.TryGetProperty("texte", out var t) ? t.GetString() ?? "" : "",
                    r.TryGetProperty("domaine", out var d) ? d.GetString() ?? "" : ""));
            }
        }

        var alertes = new List<string>();
        if (root.TryGetProperty("alertes", out var alertEl))
        {
            foreach (var a in alertEl.EnumerateArray())
            {
                var text = a.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                    alertes.Add(text);
            }
        }

        return new SyntheseContenuNarratif(
            ProfilType: root.TryGetProperty("profilType", out var pt) ? pt.GetString() ?? "Réactif" : "Réactif",
            ProfilTexte: root.TryGetProperty("profilTexte", out var ptx) ? ptx.GetString() : null,
            IndiceCommentaire: root.TryGetProperty("indiceCommentaire", out var ic) ? ic.GetString() : null,
            MaturiteCommentaire: root.TryGetProperty("maturiteCommentaire", out var mc) ? mc.GetString() : null,
            Recommandations: recos,
            Alertes: alertes,
            GenereeParIa: true);
    }

    /// <summary>Texte de repli algorithmique (pas d'IA) — mêmes formulations que <c>GdtSyntheseIaService</c> dans mvp, jamais une erreur brute affichée à l'utilisateur.</summary>
    public static SyntheseContenuNarratif BuildFallback(ProfilPsychosocial p, string profilType, int indice, int maturite)
    {
        var recos = new List<RecommandationIa>();
        var prio = 1;
        void Add(string domaine, string texte) => recos.Add(new RecommandationIa(prio++, texte, domaine));

        if (p.Desequilibres.Contains("Trop de temps professionnel"))
            Add("pro", "Définir des heures limites de travail et introduire des blocs de récupération.");
        if (p.Desequilibres.Contains("Pas assez de temps personnel"))
            Add("perso", "Réserver un créneau quotidien fixe dédié à vos besoins personnels.");
        if (p.EmotionsNegatives.Count >= 3)
            Add("organisation", "Externaliser certaines tâches administratives et introduire des pauses de recentrage.");
        if (p.ToleranceImprevu == "Anxieux")
            Add("organisation", "Intégrer des marges tampons dans votre planning pour plus de flexibilité.");
        if (p.InterruptionsTravail is "Souvent" or "Constamment")
            Add("pro", "Protéger des blocs horaires dédiés pour retrouver une meilleure concentration.");
        if (maturite <= 2)
            Add("organisation", "Mettre en place un agenda structuré et planifier la veille pour le lendemain.");
        if (p.OuiTropFacile is "Souvent" or "Toujours")
            Add("organisation", "Pratiquer la méthode du délai de réponse : ne jamais accepter immédiatement.");
        if (recos.Count == 0)
            Add("organisation", "Votre profil est globalement équilibré. Continuez à maintenir vos bonnes habitudes.");

        return new SyntheseContenuNarratif(
            ProfilType: profilType,
            ProfilTexte: BuildFallbackProfilTexte(profilType),
            IndiceCommentaire: BuildFallbackIndiceTexte(indice),
            MaturiteCommentaire: BuildFallbackMaturiteTexte(maturite),
            Recommandations: recos,
            Alertes: [],
            GenereeParIa: false);
    }

    private static string BuildFallbackProfilTexte(string profilType) => profilType switch
    {
        "Anticipatif" => "Vous planifiez naturellement vos activités. Cette approche vous offre une bonne maîtrise du temps.",
        "Structuré" => "Votre rapport au temps est stable et organisé. Vous fonctionnez avec des routines efficaces.",
        "Saturé" => "Votre temps est fortement sollicité. Une redistribution des priorités est recommandée.",
        "Fragmenté" => "Vos journées comportent de nombreuses interruptions. Des blocs de temps protégés amélioreraient votre concentration.",
        _ => "Votre gestion du temps repose sur la réaction aux événements. Une structure plus claire renforcerait votre efficacité.",
    };

    private static string BuildFallbackIndiceTexte(int indice) => indice switch
    {
        <= 25 => "Certains domaines essentiels semblent insuffisants. Une réorganisation prioritaire est nécessaire.",
        <= 50 => "Votre équilibre temporel est fragile. Une réflexion sur vos priorités pourrait améliorer votre qualité de vie.",
        <= 75 => "Votre gestion du temps est fonctionnelle, mais quelques ajustements ciblés renforceront votre stabilité.",
        <= 90 => "Vous parvenez à maintenir un bon équilibre. Quelques optimisations mineures pourraient encore l'améliorer.",
        _ => "Votre gestion du temps est exemplaire. Continuez à maintenir cette cohérence.",
    };

    private static string BuildFallbackMaturiteTexte(int maturite) => maturite switch
    {
        1 => "Aucune structure, réactivité totale. Mettre en place des routines simples pourrait transformer votre quotidien.",
        2 => "Vous avez des intentions d'organisation, mais elles manquent de constance.",
        3 => "Organisation correcte et fonctionnelle. En renforçant la priorisation, vous gagneriez en efficacité.",
        4 => "Organisation solide. Vous savez planifier, anticiper et gérer vos priorités.",
        5 => "Gestion du temps fluide, cohérente et efficace. Maturité organisationnelle élevée.",
        _ => "",
    };
}
