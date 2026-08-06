namespace Spectrometre.Modules.GestionDuTemps.Entities;

/// <summary>
/// Traduction d'affichage du <c>ProfilType</c> calculé par <c>SyntheseCalculator</c> (bilinguisme, cycle
/// contenu métier). La valeur canonique française ("Réactif", "Anticipatif", "Structuré", "Saturé",
/// "Fragmenté") reste inchangée en interne : elle est persistée (<c>Synthese.ProfilType</c>) et utilisée comme
/// clé de branchement par <c>SyntheseNarrativeBuilder</c> et le schéma JSON envoyé au modèle d'IA — seule la
/// représentation à l'écran change de langue. Traduction automatique pour l'instant, à affiner plus tard.
/// </summary>
public static class SyntheseProfilTypeLabels
{
    private static readonly IReadOnlyDictionary<string, string> LabelsEn = new Dictionary<string, string>
    {
        ["Réactif"] = "Reactive",
        ["Anticipatif"] = "Anticipatory",
        ["Structuré"] = "Structured",
        ["Saturé"] = "Saturated",
        ["Fragmenté"] = "Fragmented",
    };

    public static string Display(string profilType, bool english) =>
        english && LabelsEn.TryGetValue(profilType, out var translated) ? translated : profilType;
}
