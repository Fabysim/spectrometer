namespace Spectrometre.Modules.Entretien.Entities;

/// <summary>Catégorie de la bibliothèque de questions d'entrevue (schéma public partagé).</summary>
public sealed class InterviewQuestionCategory
{
    public int Id { get; set; }
    public string? SeedKey { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }

    public ICollection<InterviewQuestionSubCategory> SubCategories { get; set; } =
        new List<InterviewQuestionSubCategory>();
}
