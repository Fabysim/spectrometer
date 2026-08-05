using Microsoft.EntityFrameworkCore;
using Spectrometre.Modules.ProfilCandidat.Data;
using Spectrometre.Modules.ProfilCandidat.Entities;

namespace Spectrometre.Modules.ProfilCandidat.Services;

public sealed class CandidateProfileService(ProfilCandidatDbContext db) : ICandidateProfileService
{
    public async Task<int> GetOrCreateProfileIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var existing = await db.CandidateProfiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (existing is not null)
            return existing.Id;

        var profile = new CandidateProfile { UserId = userId };
        db.CandidateProfiles.Add(profile);
        await db.SaveChangesAsync(cancellationToken);
        return profile.Id;
    }

    public async Task<IReadOnlyList<CandidateQuestionView>> GetQuestionnaireAsync(int candidateProfileId, CancellationToken cancellationToken = default)
    {
        var answers = await db.CandidateAnswers
            .Where(a => a.CandidateProfileId == candidateProfileId)
            .ToDictionaryAsync(a => a.QuestionId, cancellationToken);

        var questions = await db.CandidateQuestions
            .Include(q => q.Examples)
            .OrderBy(q => q.Number)
            .ToListAsync(cancellationToken);

        return questions.Select(q =>
        {
            answers.TryGetValue(q.Id, out var answer);
            return new CandidateQuestionView(
                q.Id,
                q.Theme,
                q.Number,
                q.Text,
                q.Examples.OrderBy(e => e.DisplayOrder).Select(e => e.Text).ToList(),
                answer?.AnswerText,
                answer?.UpdatedAt);
        }).ToList();
    }

    public async Task SaveAnswerAsync(int candidateProfileId, int questionId, string? answerText, CancellationToken cancellationToken = default)
    {
        var answer = await db.CandidateAnswers
            .FirstOrDefaultAsync(a => a.CandidateProfileId == candidateProfileId && a.QuestionId == questionId, cancellationToken);

        if (answer is null)
        {
            db.CandidateAnswers.Add(new CandidateAnswer
            {
                CandidateProfileId = candidateProfileId,
                QuestionId = questionId,
                AnswerText = answerText,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        }
        else
        {
            answer.AnswerText = answerText;
            answer.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static readonly Dictionary<CandidateTheme, SynthesisCategory> ThemeToCategory = new()
    {
        [CandidateTheme.ActivitesInterets] = SynthesisCategory.Interet,
        [CandidateTheme.TalentsNaturels] = SynthesisCategory.Talent,
        [CandidateTheme.CompetencesAcquises] = SynthesisCategory.Competence,
        [CandidateTheme.ValeursTravail] = SynthesisCategory.Valeur,
        [CandidateTheme.EnvironnementsFavorables] = SynthesisCategory.EnvironnementFavorable,
        [CandidateTheme.SignauxAlerte] = SynthesisCategory.SignalAlerte,
    };

    private static readonly char[] FragmentSeparators = ['.', ',', ';', ':'];

    /// <summary>
    /// Synthèse heuristique simple (pas d'IA) : découpe les réponses libres de chaque thème en courts
    /// fragments et retient les plus courts comme tags représentatifs. Volontairement basique pour ce
    /// premier cycle — à affiner plus tard (mots-clés pondérés, IA) sans changer le contrat du service.
    /// </summary>
    public async Task<CandidateSynthesisView> GenerateSynthesisAsync(int candidateProfileId, CancellationToken cancellationToken = default)
    {
        var questions = await db.CandidateQuestions.AsNoTracking().ToListAsync(cancellationToken);
        var answers = await db.CandidateAnswers
            .AsNoTracking()
            .Where(a => a.CandidateProfileId == candidateProfileId && a.AnswerText != null && a.AnswerText != "")
            .ToListAsync(cancellationToken);

        var questionThemeById = questions.ToDictionary(q => q.Id, q => q.Theme);

        var tagsByTheme = new Dictionary<CandidateTheme, List<string>>();
        foreach (var answer in answers)
        {
            if (!questionThemeById.TryGetValue(answer.QuestionId, out var theme))
                continue;

            var fragments = ExtractFragments(answer.AnswerText!);
            if (!tagsByTheme.TryGetValue(theme, out var list))
                tagsByTheme[theme] = list = [];
            list.AddRange(fragments);
        }

        var existingTags = await db.CandidateSynthesisTags
            .Where(t => t.CandidateProfileId == candidateProfileId)
            .ToListAsync(cancellationToken);
        db.CandidateSynthesisTags.RemoveRange(existingTags);

        var result = new Dictionary<SynthesisCategory, IReadOnlyList<string>>();
        foreach (var (theme, category) in ThemeToCategory)
        {
            var tags = tagsByTheme.TryGetValue(theme, out var raw)
                ? raw.Select(Capitalize).Distinct(StringComparer.OrdinalIgnoreCase).Take(5).ToList()
                : [];

            result[category] = tags;
            foreach (var tag in tags)
                db.CandidateSynthesisTags.Add(new CandidateSynthesisTag { CandidateProfileId = candidateProfileId, Category = category, Label = tag });
        }

        await db.SaveChangesAsync(cancellationToken);
        return new CandidateSynthesisView(result, DateTimeOffset.UtcNow);
    }

    public async Task<CandidateSynthesisView?> GetLastSynthesisAsync(int candidateProfileId, CancellationToken cancellationToken = default)
    {
        var tags = await db.CandidateSynthesisTags
            .AsNoTracking()
            .Where(t => t.CandidateProfileId == candidateProfileId)
            .ToListAsync(cancellationToken);

        if (tags.Count == 0)
            return null;

        var byCategory = Enum.GetValues<SynthesisCategory>().ToDictionary(
            c => c,
            c => (IReadOnlyList<string>)tags.Where(t => t.Category == c).Select(t => t.Label).ToList());

        return new CandidateSynthesisView(byCategory, DateTimeOffset.UtcNow);
    }

    public async Task SaveCompatibilityCriteriaAsync(int candidateProfileId, CandidateCompatibilityCriteriaView criteria, CancellationToken cancellationToken = default)
    {
        var entity = await db.CandidateCompatibilityCriteria
            .FirstOrDefaultAsync(c => c.CandidateProfileId == candidateProfileId, cancellationToken);

        if (entity is null)
        {
            entity = new CandidateCompatibilityCriteria { CandidateProfileId = candidateProfileId };
            db.CandidateCompatibilityCriteria.Add(entity);
        }

        entity.TechniqueTags = criteria.TechniqueTags.ToList();
        entity.ComportementaleTags = criteria.ComportementaleTags.ToList();
        entity.CulturelleTags = criteria.CulturelleTags.ToList();
        entity.RythmeTravail = criteria.RythmeTravail;
        entity.MotivationnelleTags = criteria.MotivationnelleTags.ToList();
        entity.PointsVigilanceTags = criteria.PointsVigilanceTags.ToList();
        entity.TechniqueNotes = criteria.TechniqueNotes;
        entity.ComportementaleNotes = criteria.ComportementaleNotes;
        entity.CulturelleNotes = criteria.CulturelleNotes;
        entity.OrganisationnelleNotes = criteria.OrganisationnelleNotes;
        entity.MotivationnelleNotes = criteria.MotivationnelleNotes;
        entity.PointsVigilanceNotes = criteria.PointsVigilanceNotes;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CandidateCompatibilityCriteriaView?> GetCompatibilityCriteriaAsync(int candidateProfileId, CancellationToken cancellationToken = default)
    {
        var entity = await db.CandidateCompatibilityCriteria
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CandidateProfileId == candidateProfileId, cancellationToken);

        return entity is null
            ? null
            : new CandidateCompatibilityCriteriaView(
                entity.TechniqueTags, entity.ComportementaleTags, entity.CulturelleTags, entity.RythmeTravail,
                entity.MotivationnelleTags, entity.PointsVigilanceTags,
                entity.TechniqueNotes, entity.ComportementaleNotes, entity.CulturelleNotes,
                entity.OrganisationnelleNotes, entity.MotivationnelleNotes, entity.PointsVigilanceNotes);
    }

    private static readonly string[] ConjunctionSeparators = [" et ", " ou ", " mais ", " ainsi que "];

    /// <summary>
    /// Découpe une réponse libre en courts fragments-tags. Sépare d'abord sur la ponctuation, puis sur
    /// les conjonctions courantes pour les phrases longues (les réponses réelles sont souvent une seule
    /// phrase, pas une liste à virgules). Si aucun fragment n'est assez court, retient un extrait du
    /// début de phrase plutôt que de renvoyer une synthèse vide.
    /// </summary>
    private static List<string> ExtractFragments(string answerText)
    {
        var candidates = answerText
            .Split(FragmentSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(f => f.Split(ConjunctionSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToList();

        var shortEnough = candidates.Where(f => f.Length is >= 3 and <= 60).ToList();
        if (shortEnough.Count > 0)
            return shortEnough;

        // Repli : aucun fragment assez court — on retient un extrait des premiers mots plutôt que rien.
        var longest = candidates.OrderByDescending(f => f.Length).FirstOrDefault();
        if (longest is null)
            return [];

        var words = longest.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(5);
        return [string.Join(' ', words)];
    }

    private static string Capitalize(string text) =>
        text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];
}
