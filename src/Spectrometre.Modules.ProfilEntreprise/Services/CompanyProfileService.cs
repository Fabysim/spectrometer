using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.ProfilEntreprise.Data;
using Spectrometre.Modules.ProfilEntreprise.Entities;

namespace Spectrometre.Modules.ProfilEntreprise.Services;

/// <summary>
/// Instancie un <see cref="ProfilEntrepriseDbContext"/> frais à chaque opération via
/// <see cref="IDbContextFactory{TContext}"/> plutôt que d'injecter le DbContext directement : en Blazor
/// Server le service (scoped) vit pour tout le circuit, donc un DbContext injecté au constructeur
/// figerait son modèle EF (schéma tenant) sur la première entreprise active de la session. Le schéma
/// courant (<see cref="ITenantContext"/>, lui-même scoped) est appliqué juste après la création.
/// </summary>
public sealed class CompanyProfileService(IDbContextFactory<ProfilEntrepriseDbContext> dbFactory, ITenantContext tenantContext) : ICompanyProfileService
{
    private async Task<ProfilEntrepriseDbContext> CreateDbAsync(CancellationToken cancellationToken)
    {
        var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        db.TenantSchema = tenantContext.SchemaName;
        return db;
    }

    public async Task<int> GetOrCreateProfileIdAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbAsync(cancellationToken);

        var existing = await db.CompanyProfiles.FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
            return existing.Id;

        var profile = new CompanyProfile();
        db.CompanyProfiles.Add(profile);
        await db.SaveChangesAsync(cancellationToken);
        return profile.Id;
    }

    public async Task<IReadOnlyList<CompanyQuestionView>> GetQuestionnaireAsync(int companyProfileId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbAsync(cancellationToken);

        var answers = await db.CompanyAnswers
            .Where(a => a.CompanyProfileId == companyProfileId)
            .ToDictionaryAsync(a => a.QuestionId, cancellationToken);

        var questions = await db.CompanyQuestions.OrderBy(q => q.Number).ToListAsync(cancellationToken);

        return questions.Select(q =>
        {
            answers.TryGetValue(q.Id, out var answer);
            return new CompanyQuestionView(q.Id, q.Theme, q.Number, q.Text, answer?.AnswerText, answer?.UpdatedAt);
        }).ToList();
    }

    public async Task SaveAnswerAsync(int companyProfileId, int questionId, string? answerText, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbAsync(cancellationToken);

        var answer = await db.CompanyAnswers
            .FirstOrDefaultAsync(a => a.CompanyProfileId == companyProfileId && a.QuestionId == questionId, cancellationToken);

        if (answer is null)
        {
            db.CompanyAnswers.Add(new CompanyAnswer
            {
                CompanyProfileId = companyProfileId,
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

    public async Task SaveCompatibilityCriteriaAsync(int companyProfileId, CompanyCompatibilityCriteriaView criteria, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbAsync(cancellationToken);

        var entity = await db.CompanyCompatibilityCriteria
            .FirstOrDefaultAsync(c => c.CompanyProfileId == companyProfileId, cancellationToken);

        if (entity is null)
        {
            entity = new CompanyCompatibilityCriteria { CompanyProfileId = companyProfileId };
            db.CompanyCompatibilityCriteria.Add(entity);
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

    public async Task<CompanyCompatibilityCriteriaView?> GetCompatibilityCriteriaAsync(int companyProfileId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbAsync(cancellationToken);

        var entity = await db.CompanyCompatibilityCriteria
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CompanyProfileId == companyProfileId, cancellationToken);

        return entity is null
            ? null
            : new CompanyCompatibilityCriteriaView(
                entity.TechniqueTags, entity.ComportementaleTags, entity.CulturelleTags, entity.RythmeTravail,
                entity.MotivationnelleTags, entity.PointsVigilanceTags,
                entity.TechniqueNotes, entity.ComportementaleNotes, entity.CulturelleNotes,
                entity.OrganisationnelleNotes, entity.MotivationnelleNotes, entity.PointsVigilanceNotes);
    }
}
