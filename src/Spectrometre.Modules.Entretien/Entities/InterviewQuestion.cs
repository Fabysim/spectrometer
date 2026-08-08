namespace Spectrometre.Modules.Entretien.Entities;

/// <summary>Question de la bibliothèque globale d'entrevue (schéma public partagé).</summary>
public sealed class InterviewQuestion
{
    public int Id { get; set; }
    public int InterviewQuestionSubCategoryId { get; set; }
    public InterviewQuestionSubCategory? SubCategory { get; set; }
    public string? SeedKey { get; set; }
    public string Text { get; set; } = "";
    public string? ExpectedElements { get; set; }
    public int SortOrder { get; set; }
}
