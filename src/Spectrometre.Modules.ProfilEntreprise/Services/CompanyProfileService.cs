using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Compatibility;
using Spectrometre.Core.Suivi;
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
public sealed class CompanyProfileService(IDbContextFactory<ProfilEntrepriseDbContext> dbFactory, ITenantContext tenantContext, IProfileChangeRecorder changeRecorder) : ICompanyProfileService
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

        // Bilinguisme (cycle contenu métier) : Text/TextEn choisis selon la culture courante — jamais
        // AnswerText, qui reste la réponse libre de l'entreprise, dans la langue où elle l'a saisie.
        var english = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "fr";

        return questions.Select(q =>
        {
            answers.TryGetValue(q.Id, out var answer);
            return new CompanyQuestionView(q.Id, q.Theme, q.Number, english ? q.TextEn ?? q.Text : q.Text, answer?.AnswerText, answer?.UpdatedAt);
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

    public async Task ToggleTagAsync(int companyProfileId, CriteriaField field, string tag, bool isChecked, CancellationToken cancellationToken = default)
    {
        var (oldValue, newValue) = await MutateCriteriaAsync(companyProfileId, entity =>
        {
            var tags = TagsFor(entity, field);
            var before = string.Join(", ", tags);
            if (isChecked) { if (!tags.Contains(tag)) tags.Add(tag); }
            else tags.Remove(tag);
            return (before, string.Join(", ", tags));
        }, cancellationToken);

        await changeRecorder.RecordChangeAsync(companyProfileId, ProfileOwnerType.Entreprise, $"{field}.Tags", oldValue, newValue, DateTimeOffset.UtcNow, cancellationToken);
    }

    public async Task SetRythmeTravailAsync(int companyProfileId, int? rythme, CancellationToken cancellationToken = default)
    {
        var (oldValue, newValue) = await MutateCriteriaAsync(companyProfileId, entity =>
        {
            var before = entity.RythmeTravail?.ToString();
            entity.RythmeTravail = rythme;
            return (before, rythme?.ToString());
        }, cancellationToken);

        await changeRecorder.RecordChangeAsync(companyProfileId, ProfileOwnerType.Entreprise, "Organisationnelle.Rythme", oldValue, newValue, DateTimeOffset.UtcNow, cancellationToken);
    }

    public async Task SetNotesAsync(int companyProfileId, CriteriaField field, string? notes, CancellationToken cancellationToken = default)
    {
        var (oldValue, newValue) = await MutateCriteriaAsync(companyProfileId, entity =>
        {
            var before = NotesFor(entity, field);
            SetNotesFor(entity, field, notes);
            return (before, notes);
        }, cancellationToken);

        await changeRecorder.RecordChangeAsync(companyProfileId, ProfileOwnerType.Entreprise, $"{field}.Notes", oldValue, newValue, DateTimeOffset.UtcNow, cancellationToken);
    }

    private static List<string> TagsFor(CompanyCompatibilityCriteria entity, CriteriaField field) => field switch
    {
        CriteriaField.Technique => entity.TechniqueTags,
        CriteriaField.Comportementale => entity.ComportementaleTags,
        CriteriaField.Culturelle => entity.CulturelleTags,
        CriteriaField.Motivationnelle => entity.MotivationnelleTags,
        CriteriaField.PointsVigilance => entity.PointsVigilanceTags,
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, "L'axe organisationnel n'a pas de tags — seulement un rythme de travail (1-5)."),
    };

    private static void SetNotesFor(CompanyCompatibilityCriteria entity, CriteriaField field, string? notes)
    {
        switch (field)
        {
            case CriteriaField.Technique: entity.TechniqueNotes = notes; break;
            case CriteriaField.Comportementale: entity.ComportementaleNotes = notes; break;
            case CriteriaField.Culturelle: entity.CulturelleNotes = notes; break;
            case CriteriaField.Organisationnelle: entity.OrganisationnelleNotes = notes; break;
            case CriteriaField.Motivationnelle: entity.MotivationnelleNotes = notes; break;
            case CriteriaField.PointsVigilance: entity.PointsVigilanceNotes = notes; break;
            default: throw new ArgumentOutOfRangeException(nameof(field), field, null);
        }
    }

    private static string? NotesFor(CompanyCompatibilityCriteria entity, CriteriaField field) => field switch
    {
        CriteriaField.Technique => entity.TechniqueNotes,
        CriteriaField.Comportementale => entity.ComportementaleNotes,
        CriteriaField.Culturelle => entity.CulturelleNotes,
        CriteriaField.Organisationnelle => entity.OrganisationnelleNotes,
        CriteriaField.Motivationnelle => entity.MotivationnelleNotes,
        CriteriaField.PointsVigilance => entity.PointsVigilanceNotes,
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, null),
    };

    /// <summary>
    /// Voir le commentaire détaillé sur <c>CandidateProfileService.MutateCriteriaAsync</c> (module Profil
    /// Candidat) : même correctif de perte de mise à jour, même stratégie (mutation ciblée sur un seul
    /// champ + jeton de concurrence optimiste xmin + relecture/réapplication en cas de conflit). Ce
    /// service utilisait déjà <see cref="IDbContextFactory{TContext}"/> (instance fraîche par appel, pour
    /// le multi-tenant) : le bug ici était donc purement une course entre plusieurs requêtes concurrentes,
    /// pas un usage concurrent d'un même DbContext.
    /// </summary>
    /// <summary>
    /// <paramref name="applyChange"/> retourne (ancienne valeur, nouvelle valeur) EN TEXTE, capturées sur
    /// la tentative qui réussit réellement — utilisées par l'appelant pour tracer le changement via
    /// <see cref="IProfileChangeRecorder"/> (module Suivi Évolutif) sans dupliquer la logique de sauvegarde.
    /// </summary>
    private async Task<(string? Old, string? New)> MutateCriteriaAsync(int companyProfileId, Func<CompanyCompatibilityCriteria, (string? Old, string? New)> applyChange, CancellationToken cancellationToken)
    {
        // Voir le commentaire équivalent côté CandidateProfileService.MutateCriteriaAsync : maxAttempts
        // généreux + délai aléatoire entre tentatives pour désynchroniser les conflits sous forte contention.
        const int maxAttempts = 30;
        var random = Random.Shared;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            await using var db = await CreateDbAsync(cancellationToken);

            var entity = await db.CompanyCompatibilityCriteria
                .FirstOrDefaultAsync(c => c.CompanyProfileId == companyProfileId, cancellationToken);

            var isNew = entity is null;
            if (entity is null)
            {
                entity = new CompanyCompatibilityCriteria { CompanyProfileId = companyProfileId };
                db.CompanyCompatibilityCriteria.Add(entity);
            }

            var changeValues = applyChange(entity);
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return changeValues;
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
            {
                await Task.Delay(random.Next(5, 30), cancellationToken);
            }
            catch (DbUpdateException ex) when (isNew && attempt < maxAttempts && IsUniqueViolation(ex))
            {
                await Task.Delay(random.Next(5, 30), cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"Impossible d'enregistrer les critères de compatibilité de l'entreprise {companyProfileId} après {maxAttempts} tentatives (conflits de concurrence répétés).");
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation };

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
