using Spectrometre.Modules.JeunesPrestataires.Entities;

namespace Spectrometre.Modules.JeunesPrestataires.Catalog;

/// <summary>
/// Écran 2 du document Bouchra (catégories selon le profil) : 5 questions d'orientation,
/// distinctes de Part0/Part1/Part2. Affiché une fois au premier accès jeune à
/// l'auto-observation — c'est là que <see cref="AutoObservationCatalog.GetPartiesOrdonnees"/>
/// s'applique, donc le moment le plus cohérent. Pas dans <see cref="AutoObservationCatalog.AllSections"/>.
/// Réponses stockées dans <c>AutoObservationReponses</c> (clés <c>orientation.q*</c>) ;
/// « déjà vu » via <c>AutoObservationSectionProgress.SectionKey == <see cref="SectionKey"/></c>
/// (y compris si le jeune passe sans répondre).
/// </summary>
public sealed record AutoObservationOrientationQuestionDef(
    string Key,
    string LabelResourceKey,
    IReadOnlyList<string> OptionCodes);

public static class AutoObservationOrientationCatalog
{
    public const string SectionKey = "orientation";

    public const string Oui = "oui";
    public const string Non = "non";
    public const string UnPeu = "un_peu";
    public const string JeNeSaisPas = "je_ne_sais_pas";

    public const string Q1 = "orientation.q1";
    public const string Q2 = "orientation.q2";
    public const string Q3 = "orientation.q3";
    public const string Q4 = "orientation.q4";
    public const string Q5 = "orientation.q5";

    public static IReadOnlyList<AutoObservationOrientationQuestionDef> Questions { get; } =
    [
        new(Q1, "Orientation_Q1", [Oui, Non, UnPeu]),
        new(Q2, "Orientation_Q2", [Oui, Non]),
        new(Q3, "Orientation_Q3", [Oui, Non]),
        new(Q4, "Orientation_Q4", [Oui, Non]),
        new(Q5, "Orientation_Q5", [Oui, Non, JeNeSaisPas]),
    ];

    public static IReadOnlySet<string> QuestionKeys { get; } =
        Questions.Select(q => q.Key).ToHashSet(StringComparer.Ordinal);

    public static bool EstReponseValide(string questionKey, string? optionCode)
    {
        var q = Questions.FirstOrDefault(x => x.Key == questionKey);
        return q is not null
               && !string.IsNullOrWhiteSpace(optionCode)
               && q.OptionCodes.Contains(optionCode, StringComparer.Ordinal);
    }

    /// <summary>
    /// <see cref="ProfilAccompagnement.Autonome"/> uniquement si le signal est univoque :
    /// expérience réelle et rémunérée (Q1=oui, pas « un peu » ; Q2=oui) ET objectif d'orientation
    /// plutôt que de découverte (Q3=non, Q4=oui) ET pas de préférence pour les petites missions
    /// d'abord (Q5=non). Toute autre combinaison — réponses mixtes, « un peu », « je ne sais pas »,
    /// Q3 et Q4 tous deux oui (deux objectifs « principaux »), réponses manquantes ou invalides —
    /// retombe sur <see cref="ProfilAccompagnement.SansExperience"/> (Catégorie A du document :
    /// sécuriser les habitudes de travail). Jamais Autonome par défaut.
    /// </summary>
    public static ProfilAccompagnement SuggereProfil(IReadOnlyDictionary<string, string?> reponses)
    {
        if (reponses.TryGetValue(Q1, out var q1) && q1 == Oui
            && reponses.TryGetValue(Q2, out var q2) && q2 == Oui
            && reponses.TryGetValue(Q3, out var q3) && q3 == Non
            && reponses.TryGetValue(Q4, out var q4) && q4 == Oui
            && reponses.TryGetValue(Q5, out var q5) && q5 == Non)
            return ProfilAccompagnement.Autonome;

        return ProfilAccompagnement.SansExperience;
    }
}
