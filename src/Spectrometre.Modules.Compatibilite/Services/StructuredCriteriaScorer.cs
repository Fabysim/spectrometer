namespace Spectrometre.Modules.Compatibilite.Services;

/// <summary>
/// Scoring sur critères structurés (tags choisis dans un vocabulaire partagé, échelle 1-5) — remplace
/// l'ancien <c>TextSimilarityScorer</c> (recouvrement lexical sur texte libre). L'audit qui a précédé ce
/// changement a montré que deux réponses décrivant la même réalité avec des mots différents (accords,
/// synonymes) obtenaient un recouvrement de mots quasi nul (ex. « respecte » vs « respect », 18% de
/// similarité mesurée pour deux réponses en réalité compatibles). Comparer des tags identiques choisis
/// dans la même liste élimine ce problème à la racine : le score reflète un vrai accord/désaccord de
/// contenu, plus un artefact de formulation.
/// </summary>
internal static class StructuredCriteriaScorer
{
    /// <summary>Indice de Jaccard sur deux ensembles de tags (comparaison insensible à la casse). Neutre si l'un des deux est vide (profil incomplet — pas de pénalité injuste).</summary>
    public static int TagOverlapScore(IReadOnlyList<string> candidateTags, IReadOnlyList<string> companyTags, int neutralScore = 50)
    {
        if (candidateTags.Count == 0 || companyTags.Count == 0)
            return neutralScore;

        var a = new HashSet<string>(candidateTags, StringComparer.OrdinalIgnoreCase);
        var b = new HashSet<string>(companyTags, StringComparer.OrdinalIgnoreCase);

        var intersection = a.Intersect(b, StringComparer.OrdinalIgnoreCase).Count();
        var union = a.Union(b, StringComparer.OrdinalIgnoreCase).Count();

        return union == 0 ? neutralScore : (int)Math.Round(100.0 * intersection / union);
    }

    /// <summary>Écart sur une échelle 1-5 : 100 - |écart| × 25 (écart 0 → 100%, écart 4 → 0%). Neutre si l'une des deux valeurs n'est pas renseignée.</summary>
    public static int ScaleScore(int? candidateValue, int? companyValue, int neutralScore = 50)
    {
        if (candidateValue is null || companyValue is null)
            return neutralScore;

        var ecart = Math.Abs(candidateValue.Value - companyValue.Value);
        return Math.Max(0, 100 - ecart * 25);
    }

    /// <summary>Tags de points de vigilance signalés des deux côtés à la fois — un vrai risque partagé, pas une heuristique par mot-clé.</summary>
    public static IReadOnlyList<string> SharedVigilanceTags(IReadOnlyList<string> candidateTags, IReadOnlyList<string> companyTags) =>
        candidateTags.Intersect(companyTags, StringComparer.OrdinalIgnoreCase).ToList();
}
