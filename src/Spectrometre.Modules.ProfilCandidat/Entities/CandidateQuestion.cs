namespace Spectrometre.Modules.ProfilCandidat.Entities;

/// <summary>Catalogue de référence (seedé) des questions du questionnaire — texte repris tel quel du document source.</summary>
public sealed class CandidateQuestion
{
    public int Id { get; set; }
    public CandidateTheme Theme { get; set; }

    /// <summary>Numéro affiché dans le document source (1 à 26, unique dans le périmètre A-F retenu).</summary>
    public int Number { get; set; }

    public required string Text { get; set; }

    public ICollection<CandidateQuestionExample> Examples { get; set; } = new List<CandidateQuestionExample>();
}

/// <summary>Exemple de réponse suggéré (repris du document), affiché en placeholder dans l'UI — pas une case à cocher : la réponse reste libre.</summary>
public sealed class CandidateQuestionExample
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public required string Text { get; set; }
    public int DisplayOrder { get; set; }
}
