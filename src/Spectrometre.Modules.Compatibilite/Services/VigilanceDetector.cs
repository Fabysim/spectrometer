namespace Spectrometre.Modules.Compatibilite.Services;

/// <summary>
/// Détection simple par mots-clés d'incompatibilités déclarées (ex. « rythme de travail très soutenu »
/// côté entreprise vs « évite la pression » côté candidat), en plus des points de vigilance générés
/// pour un score d'axe faible. Liste de paires volontairement courte et explicite — à enrichir avec le
/// vocabulaire réellement observé une fois en production.
/// </summary>
internal static class VigilanceDetector
{
    private static readonly (string CompanyKeyword, string CandidateKeyword, string Message)[] KnownIncompatibilities =
    [
        ("pression", "pression", "L'entreprise mentionne un rythme sous pression ; le candidat signale une sensibilité à la pression."),
        ("rythme soutenu", "pression", "Rythme de travail soutenu côté entreprise ; à valider avec le candidat en entretien."),
        ("urgence", "changement", "Gestion fréquente de l'urgence côté entreprise ; le candidat signale une difficulté avec les changements imprévus."),
        ("bruit", "bruit", "Environnement bruyant signalé par l'entreprise ; le candidat y est sensible."),
        ("isolement", "isolement", "Travail isolé possible ; le candidat signale un besoin de contact humain."),
        ("conflit", "conflit", "Gestion de conflits fréquente signalée par l'entreprise ; point sensible pour le candidat."),
        ("horaires variables", "horaires", "Horaires variables côté entreprise ; à confirmer avec les contraintes du candidat."),
        ("controle", "autonomie", "Supervision rapprochée côté entreprise ; le candidat exprime un besoin d'autonomie."),
    ];

    public static List<string> Detect(string? companyText, string? candidateText)
    {
        if (string.IsNullOrWhiteSpace(companyText) || string.IsNullOrWhiteSpace(candidateText))
            return [];

        var companyLower = companyText.ToLowerInvariant();
        var candidateLower = candidateText.ToLowerInvariant();

        return KnownIncompatibilities
            .Where(k => companyLower.Contains(k.CompanyKeyword, StringComparison.Ordinal)
                        && candidateLower.Contains(k.CandidateKeyword, StringComparison.Ordinal))
            .Select(k => k.Message)
            .ToList();
    }
}
