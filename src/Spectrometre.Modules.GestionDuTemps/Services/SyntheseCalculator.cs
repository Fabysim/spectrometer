using Spectrometre.Modules.GestionDuTemps.Entities;

namespace Spectrometre.Modules.GestionDuTemps.Services;

/// <summary>
/// Calcul déterministe (profilType/indiceEquilibre/niveauMaturite) — repris tel quel de
/// <c>GdtSyntheseCalculator</c> (mvp). Volontairement SANS aucun appel IA ni E/S : ces trois valeurs
/// restent toujours fiables et reproductibles, même quand la synthèse narrative (voir
/// <see cref="IAiSynthesisService"/>) est indisponible. Ne dépend que des données du module Gestion du
/// temps (le profil lui-même) — aucune référence à un autre module.
/// </summary>
internal static class SyntheseCalculator
{
    public static (string ProfilType, int IndiceEquilibre, int NiveauMaturite) Calculer(ProfilPsychosocial? profil)
    {
        if (profil is null)
            return ("Réactif", 50, 3);

        var maturite = CalculerMaturite(profil);
        var indice = CalculerIndice(profil, maturite);
        var type = CalculerProfilType(profil, maturite);
        return (type, indice, maturite);
    }

    private static int CalculerMaturite(ProfilPsychosocial p)
    {
        var score = 1;
        if (p.UtiliseAgenda == true) score++;
        if (p.PlanificationAvance is "Souvent" or "Systématiquement") score++;
        if (p.RituelsQuotidiens == true) score++;
        if (p.CapaciteNon is "Souvent" or "Facilement") score++;
        return Math.Min(score, 5);
    }

    private static int CalculerIndice(ProfilPsychosocial p, int maturite)
    {
        const int maxPossible = 6 * 4;
        var satisfactions = new[]
        {
            p.SatisfactionSommeil, p.SatisfactionPerso, p.SatisfactionPro,
            p.SatisfactionAdmin, p.SatisfactionFamille, p.SatisfactionSocial,
        };
        var sum = satisfactions.Sum(s => s ?? 0);
        var score = (int)((double)sum / maxPossible * 100);
        if (p.Desequilibres.Count >= 3) score -= 15;
        if (p.EmotionsNegatives.Count >= 3) score -= 10;
        if (maturite >= 4) score += 10;
        return Math.Clamp(score, 0, 100);
    }

    private static string CalculerProfilType(ProfilPsychosocial p, int maturite)
    {
        var anxieux = p.ToleranceImprevu == "Anxieux";
        var surcharge = p.SentimentPression is "Souvent" or "Constamment";
        var fragmente = p.InterruptionsTravail is "Souvent" or "Constamment";
        var organise = maturite >= 4;
        var anticipe = p.PlanificationAvance is "Souvent" or "Systématiquement";

        if (organise && anticipe) return "Anticipatif";
        if (organise) return "Structuré";
        if (surcharge && anxieux) return "Saturé";
        if (fragmente) return "Fragmenté";
        return "Réactif";
    }
}
