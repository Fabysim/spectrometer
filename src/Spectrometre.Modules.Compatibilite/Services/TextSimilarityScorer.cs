using System.Globalization;
using System.Text;

namespace Spectrometre.Modules.Compatibilite.Services;

/// <summary>
/// Scoring lexical simple (indice de Jaccard sur les mots significatifs), documenté et déterministe —
/// PAS d'IA/NLP. Le document source ne fournit pas de formule de pondération précise ; ce calcul
/// compare le vocabulaire des réponses libres du candidat et de l'entreprise pour un axe donné, et
/// sert de première approximation exploitable, remplaçable plus tard par une analyse sémantique.
/// </summary>
internal static class TextSimilarityScorer
{
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "le", "la", "les", "de", "des", "du", "un", "une", "et", "ou", "que", "qui", "quoi",
        "dans", "pour", "avec", "sur", "au", "aux", "ce", "ces", "cet", "cette", "son", "sa",
        "ses", "est", "sont", "plus", "tres", "etre", "avoir", "pas", "ne", "se", "je", "tu",
        "il", "elle", "nous", "vous", "ils", "elles", "mon", "ma", "mes", "ton", "ta", "tes",
        "notre", "votre", "leur", "en", "par", "comme", "si", "mais", "donc", "or", "ni", "car",
        "à", "d", "l", "qu",
    };

    /// <summary>Score 0-100. Retourne <paramref name="neutralScore"/> si l'un des deux textes est vide (profil incomplet — pas de pénalité injuste).</summary>
    public static int Score(string? candidateText, string? companyText, int neutralScore = 50)
    {
        var candidateWords = Tokenize(candidateText);
        var companyWords = Tokenize(companyText);

        if (candidateWords.Count == 0 || companyWords.Count == 0)
            return neutralScore;

        var intersection = candidateWords.Intersect(companyWords).Count();
        var union = candidateWords.Union(companyWords).Count();

        return union == 0 ? neutralScore : (int)Math.Round(100.0 * intersection / union);
    }

    private static HashSet<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var normalized = RemoveDiacritics(text.ToLowerInvariant());
        var words = new HashSet<string>(StringComparer.Ordinal);
        var current = new StringBuilder();

        void FlushWord()
        {
            if (current.Length >= 3 && !StopWords.Contains(current.ToString()))
                words.Add(current.ToString());
            current.Clear();
        }

        foreach (var c in normalized)
        {
            if (char.IsLetter(c))
                current.Append(c);
            else
                FlushWord();
        }
        FlushWord();

        return words;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString();
    }
}
