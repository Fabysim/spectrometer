namespace Spectrometre.Modules.PostesRecrutement.Entities;

/// <summary>
/// Niveau requis pour un critère de compétence d'un poste — même échelle 0–4 que
/// <c>EvaluationLevel</c> du MVP (<c>NotAtAll</c> … <c>VeryStrong</c>).
/// </summary>
public enum NiveauEvaluation
{
    PasDuTout = 0,
    Faible = 1,
    Moyen = 2,
    Fort = 3,
    TresFort = 4,
}

/// <summary>Libellés d'affichage du niveau (bilinguisme) — le niveau lui-même reste un enum stocké tel quel.</summary>
public static class NiveauEvaluationLabels
{
    public static string Label(NiveauEvaluation niveau, bool english) => niveau switch
    {
        NiveauEvaluation.PasDuTout => english ? "Not at all" : "Pas du tout",
        NiveauEvaluation.Faible => english ? "Weak" : "Faible",
        NiveauEvaluation.Moyen => english ? "Medium" : "Moyen",
        NiveauEvaluation.Fort => english ? "Strong" : "Fort",
        NiveauEvaluation.TresFort => english ? "Very strong" : "Très fort",
        _ => niveau.ToString(),
    };
}

/// <summary>
/// Critère de compétence exigé pour un poste (équivalent simplifié des
/// <c>FirstEvaluation</c>/<c>ProfileItemPosition</c> du MVP côté définition manuelle du profil poste —
/// catégorie + libellé + niveau requis, sans catalogue global ni IA).
/// </summary>
public sealed class CritereEvaluation
{
    public int Id { get; set; }
    public int PosteId { get; set; }
    public required string Categorie { get; set; }
    public required string Libelle { get; set; }
    public NiveauEvaluation NiveauRequis { get; set; } = NiveauEvaluation.Moyen;
    public int OrdreAffichage { get; set; }
}
