namespace Spectrometre.Modules.Entretien.Entities;

/// <summary>Sous-catégorie de la bibliothèque de questions d'entrevue (schéma public partagé).</summary>
public sealed class InterviewQuestionSubCategory
{
    public int Id { get; set; }
    public int InterviewQuestionCategoryId { get; set; }
    public InterviewQuestionCategory? Category { get; set; }
    public string? SeedKey { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }

    public ICollection<InterviewQuestion> Questions { get; set; } = new List<InterviewQuestion>();
}
