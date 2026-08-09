using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Compatibility;
using Spectrometre.Core.Suivi;
using Spectrometre.Modules.ProfilCandidat.Data;
using Spectrometre.Modules.ProfilCandidat.Entities;

namespace Spectrometre.Modules.ProfilCandidat.Services;

/// <summary>
/// Instancie un <see cref="ProfilCandidatDbContext"/> frais à chaque opération via
/// <see cref="IDbContextFactory{TContext}"/> plutôt que d'injecter le DbContext directement — même si ce
/// contexte n'est pas tenant-scopé, une instance UNIQUE partagée pour tout le circuit Blazor Server serait
/// utilisée concurremment par deux gestionnaires d'évènements qui se chevauchent (ex. deux cases de la
/// grille H cochées rapidement l'une après l'autre, avant que la première sauvegarde n'ait terminé) —
/// ce qu'EF Core interdit explicitement sur une même instance de DbContext.
/// </summary>
public sealed class CandidateProfileService(IDbContextFactory<ProfilCandidatDbContext> dbFactory, IProfileChangeRecorder changeRecorder) : ICandidateProfileService
{
    public async Task<int> GetOrCreateProfileIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.CandidateProfiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (existing is not null)
            return existing.Id;

        var profile = new CandidateProfile { UserId = userId };
        db.CandidateProfiles.Add(profile);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            // Même course que CoachProfileService.GetOrCreateProfileIdAsync (voir sa remarque) : Home.razor
            // et MainLayout.razor résolvent chacun indépendamment le profil candidat dès le premier rendu,
            // potentiellement en concurrence lors du pré-rendu SSR d'une même requête. Pas une vraie erreur :
            // le profil existe désormais, il suffit de le relire — sur un DbContext frais, le précédent
            // ayant sa transaction implicite avortée par l'échec de l'insertion.
            await using var freshDb = await dbFactory.CreateDbContextAsync(cancellationToken);
            return (await freshDb.CandidateProfiles.FirstAsync(p => p.UserId == userId, cancellationToken)).Id;
        }

        return profile.Id;
    }

    public async Task<int?> TryGetProfileIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.CandidateProfiles.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CandidateQuestionView>> GetQuestionnaireAsync(int candidateProfileId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var answers = await db.CandidateAnswers
            .Where(a => a.CandidateProfileId == candidateProfileId)
            .ToDictionaryAsync(a => a.QuestionId, cancellationToken);

        var questions = await db.CandidateQuestions
            .Include(q => q.Examples)
            .OrderBy(q => q.Number)
            .ToListAsync(cancellationToken);

        // Bilinguisme (cycle contenu métier) : Text/TextEn choisis selon la culture courante — jamais
        // AnswerText, qui reste la réponse libre du candidat, dans la langue où il l'a saisie.
        var english = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "fr";

        return questions.Select(q =>
        {
            answers.TryGetValue(q.Id, out var answer);
            return new CandidateQuestionView(
                q.Id,
                q.Theme,
                q.Number,
                english ? q.TextEn ?? q.Text : q.Text,
                q.Examples.OrderBy(e => e.DisplayOrder).Select(e => english ? e.TextEn ?? e.Text : e.Text).ToList(),
                answer?.AnswerText,
                answer?.UpdatedAt);
        }).ToList();
    }

    public async Task SaveAnswerAsync(int candidateProfileId, int questionId, string? answerText, CancellationToken cancellationToken = default)
    {
        var (oldValue, newValue, questionLabel) = await MutateAnswerAsync(candidateProfileId, questionId, answerText, cancellationToken);

        // Libellé lisible (le texte de la question, jamais l'identifiant technique) — cohérent avec ce
        // qu'affiche déjà /candidat/historique pour les axes de la grille H (ex. "Compétences techniques").
        // IProfileChangeRecorder ignore lui-même l'appel si ancienneValeur == nouvelleValeur (ex. l'utilisateur
        // navigue entre les questions sans rien changer) — pas besoin de le revérifier ici.
        await changeRecorder.RecordChangeAsync(candidateProfileId, ProfileOwnerType.Candidat, questionLabel, oldValue, newValue, DateTimeOffset.UtcNow, cancellationToken);
    }

    /// <summary>
    /// Même stratégie de concurrence que <see cref="MutateCriteriaAsync"/> (voir son commentaire détaillé) :
    /// mutation ciblée sur la seule réponse concernée, jeton xmin sur <c>CandidateAnswer</c>, relecture +
    /// réapplication en cas de conflit. Une réponse texte perdue par écriture concurrente est le même bug
    /// de perte de mise à jour que sur la grille H, pas un cas à part.
    /// </summary>
    private async Task<(string? Old, string? New, string QuestionLabel)> MutateAnswerAsync(int candidateProfileId, int questionId, string? answerText, CancellationToken cancellationToken)
    {
        const int maxAttempts = 30;
        var random = Random.Shared;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var question = await db.CandidateQuestions.AsNoTracking().FirstOrDefaultAsync(q => q.Id == questionId, cancellationToken);
            var questionLabel = question?.Text ?? $"Question #{questionId}";

            var answer = await db.CandidateAnswers
                .FirstOrDefaultAsync(a => a.CandidateProfileId == candidateProfileId && a.QuestionId == questionId, cancellationToken);

            var isNew = answer is null;
            var oldValue = answer?.AnswerText;

            if (answer is null)
            {
                answer = new CandidateAnswer { CandidateProfileId = candidateProfileId, QuestionId = questionId };
                db.CandidateAnswers.Add(answer);
            }

            answer.AnswerText = answerText;
            answer.UpdatedAt = DateTimeOffset.UtcNow;

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return (oldValue, answerText, questionLabel);
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
            $"Impossible d'enregistrer la réponse du candidat {candidateProfileId} à la question {questionId} après {maxAttempts} tentatives (conflits de concurrence répétés).");
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
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

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
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

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

    public async Task ToggleTagAsync(int candidateProfileId, CriteriaField field, string tag, bool isChecked, CancellationToken cancellationToken = default)
    {
        var (oldValue, newValue) = await MutateCriteriaAsync(candidateProfileId, entity =>
        {
            var tags = TagsFor(entity, field);
            var before = string.Join(", ", tags);
            if (isChecked) { if (!tags.Contains(tag)) tags.Add(tag); }
            else tags.Remove(tag);
            return (before, string.Join(", ", tags));
        }, cancellationToken);

        await changeRecorder.RecordChangeAsync(candidateProfileId, ProfileOwnerType.Candidat, $"{field}.Tags", oldValue, newValue, DateTimeOffset.UtcNow, cancellationToken);
    }

    public async Task SetRythmeTravailAsync(int candidateProfileId, int? rythme, CancellationToken cancellationToken = default)
    {
        var (oldValue, newValue) = await MutateCriteriaAsync(candidateProfileId, entity =>
        {
            var before = entity.RythmeTravail?.ToString();
            entity.RythmeTravail = rythme;
            return (before, rythme?.ToString());
        }, cancellationToken);

        await changeRecorder.RecordChangeAsync(candidateProfileId, ProfileOwnerType.Candidat, "Organisationnelle.Rythme", oldValue, newValue, DateTimeOffset.UtcNow, cancellationToken);
    }

    public async Task SetNotesAsync(int candidateProfileId, CriteriaField field, string? notes, CancellationToken cancellationToken = default)
    {
        var (oldValue, newValue) = await MutateCriteriaAsync(candidateProfileId, entity =>
        {
            var before = NotesFor(entity, field);
            SetNotesFor(entity, field, notes);
            return (before, notes);
        }, cancellationToken);

        await changeRecorder.RecordChangeAsync(candidateProfileId, ProfileOwnerType.Candidat, $"{field}.Notes", oldValue, newValue, DateTimeOffset.UtcNow, cancellationToken);
    }

    private static List<string> TagsFor(CandidateCompatibilityCriteria entity, CriteriaField field) => field switch
    {
        CriteriaField.Technique => entity.TechniqueTags,
        CriteriaField.Comportementale => entity.ComportementaleTags,
        CriteriaField.Culturelle => entity.CulturelleTags,
        CriteriaField.Motivationnelle => entity.MotivationnelleTags,
        CriteriaField.PointsVigilance => entity.PointsVigilanceTags,
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, "L'axe organisationnel n'a pas de tags — seulement un rythme de travail (1-5)."),
    };

    private static void SetNotesFor(CandidateCompatibilityCriteria entity, CriteriaField field, string? notes)
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

    private static string? NotesFor(CandidateCompatibilityCriteria entity, CriteriaField field) => field switch
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
    /// Applique une mutation CIBLÉE (un tag, le rythme, ou une note — jamais toute la grille) sur la ligne
    /// de critères du candidat, avec relecture + réapplication en cas de conflit de concurrence.
    /// </summary>
    /// <remarks>
    /// Correctif de la perte de mise à jour observée quand deux cases de la grille H sont cochées coup sur
    /// coup : l'ancienne <c>SaveCompatibilityCriteriaAsync</c> relisait puis réécrivait TOUTE la grille à
    /// chaque case cochée — deux appels concurrents pouvaient alors s'écraser mutuellement selon l'ordre
    /// d'achèvement de leur écriture, même si chacun avait "raison" au moment de sa lecture.
    /// <para/>
    /// Ici, chaque appel ne modifie QUE le champ concerné (<paramref name="applyChange"/> est une mutation
    /// isolée, pas un instantané complet), et la ligne est protégée par un jeton de concurrence optimiste
    /// (colonne système Postgres <c>xmin</c>, voir <c>ProfilCandidatDbContext</c>). Si <c>SaveChangesAsync</c>
    /// détecte qu'un autre appel a modifié la ligne entre notre lecture et notre écriture
    /// (<see cref="DbUpdateConcurrencyException"/>), on relit l'état à jour depuis la base et on
    /// RÉAPPLIQUE la même mutation ciblée par-dessus, au lieu d'écraser aveuglément avec un instantané
    /// devenu obsolète. Une violation de contrainte unique (<see cref="PostgresErrorCodes.UniqueViolation"/>)
    /// couvre le cas plus rare où deux créations concurrentes de la ligne (candidat n'ayant encore aucun
    /// critère enregistré) tentent chacune un INSERT — un jeton de concurrence ne protège que l'UPDATE.
    /// </remarks>
    /// <summary>
    /// <paramref name="applyChange"/> retourne (ancienne valeur, nouvelle valeur) EN TEXTE, capturées sur
    /// la tentative qui réussit réellement — utilisées par l'appelant pour tracer le changement via
    /// <see cref="IProfileChangeRecorder"/> (module Suivi Évolutif) sans dupliquer la logique de sauvegarde.
    /// </summary>
    private async Task<(string? Old, string? New)> MutateCriteriaAsync(int candidateProfileId, Func<CandidateCompatibilityCriteria, (string? Old, string? New)> applyChange, CancellationToken cancellationToken)
    {
        // maxAttempts volontairement généreux : sous forte contention (ex. test de concurrence avec une
        // dizaine d'écritures simultanées sur la MÊME ligne), chaque conflit ne coûte qu'un aller-retour DB
        // supplémentaire — on privilégie la certitude de convergence à la performance dans le pire cas,
        // qui reste un scénario de charge très supérieur à un usage réel (clics humains, jamais 10 à la fois).
        const int maxAttempts = 30;
        var random = Random.Shared;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var entity = await db.CandidateCompatibilityCriteria
                .FirstOrDefaultAsync(c => c.CandidateProfileId == candidateProfileId, cancellationToken);

            var isNew = entity is null;
            if (entity is null)
            {
                entity = new CandidateCompatibilityCriteria { CandidateProfileId = candidateProfileId };
                db.CandidateCompatibilityCriteria.Add(entity);
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
                // Une autre écriture a modifié la ligne entre notre lecture et notre écriture : on
                // réessaie avec une lecture fraîche, la mutation ciblée sera réappliquée par-dessus.
                // Petit délai aléatoire pour désynchroniser les tentatives concurrentes (évite que tous
                // les appelants en conflit ne retentent exactement au même instant, ce qui ferait
                // reconverger le même conflit indéfiniment sous forte contention).
                await Task.Delay(random.Next(5, 30), cancellationToken);
            }
            catch (DbUpdateException ex) when (isNew && attempt < maxAttempts && IsUniqueViolation(ex))
            {
                // Course à la création : un autre appel a inséré la ligne en premier — on réessaie,
                // le prochain tour la trouvera et fera un UPDATE au lieu d'un INSERT.
                await Task.Delay(random.Next(5, 30), cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"Impossible d'enregistrer les critères de compatibilité du candidat {candidateProfileId} après {maxAttempts} tentatives (conflits de concurrence répétés).");
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation };

    // ── Formulaire de CV (sections 1 à 8) ────────────────────────────────────

    public async Task<CvView> GetCvAsync(int candidateProfileId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var coordonnees = await db.CvCoordonnees.AsNoTracking().FirstOrDefaultAsync(c => c.CandidateProfileId == candidateProfileId, cancellationToken);
        var formations = await db.CvFormations.AsNoTracking().Where(f => f.CandidateProfileId == candidateProfileId).OrderBy(f => f.DisplayOrder).ThenBy(f => f.Id).ToListAsync(cancellationToken);
        var competencesEtudes = await db.CvCompetencesEtudes.AsNoTracking().FirstOrDefaultAsync(c => c.CandidateProfileId == candidateProfileId, cancellationToken);
        var experiences = await db.CvExperiences.AsNoTracking().Where(x => x.CandidateProfileId == candidateProfileId).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        var caracteristiques = await db.CvCaracteristiquesPersonnelles.AsNoTracking().FirstOrDefaultAsync(c => c.CandidateProfileId == candidateProfileId, cancellationToken);
        var loisirs = await db.CvLoisirs.AsNoTracking().FirstOrDefaultAsync(l => l.CandidateProfileId == candidateProfileId, cancellationToken);
        var references = await db.CvReferences.AsNoTracking().Where(r => r.CandidateProfileId == candidateProfileId).OrderBy(r => r.DisplayOrder).ThenBy(r => r.Id).ToListAsync(cancellationToken);
        var declaration = await db.CvDeclarations.AsNoTracking().FirstOrDefaultAsync(d => d.CandidateProfileId == candidateProfileId, cancellationToken);

        return new CvView(coordonnees, formations, competencesEtudes, experiences, caracteristiques, loisirs, references, declaration);
    }

    public Task SaveCoordonneesAsync(int candidateProfileId, CvCoordonnees input, CancellationToken cancellationToken = default) =>
        MutateWithRetryAsync<CvCoordonnees>(
            async db =>
            {
                var existing = await db.CvCoordonnees.FirstOrDefaultAsync(c => c.CandidateProfileId == candidateProfileId, cancellationToken);
                if (existing is not null) return (existing, false);
                var created = new CvCoordonnees { CandidateProfileId = candidateProfileId };
                db.CvCoordonnees.Add(created);
                return (created, true);
            },
            entity =>
            {
                entity.Nom = input.Nom;
                entity.Prenoms = input.Prenoms;
                entity.DateNaissance = input.DateNaissance;
                entity.LieuNaissance = input.LieuNaissance;
                entity.Nationalite = input.Nationalite;
                entity.AdresseComplete = input.AdresseComplete;
                entity.Telephone = input.Telephone;
                entity.Email = input.Email;
                entity.ProfilOuPosteRecherche = input.ProfilOuPosteRecherche;
                entity.UpdatedAt = DateTimeOffset.UtcNow;
            },
            cancellationToken);

    public Task<int> SaveFormationAsync(int candidateProfileId, int? id, CvFormation input, CancellationToken cancellationToken = default) =>
        MutateWithRetryAsync(
            async db =>
            {
                if (id is int existingId)
                {
                    var existing = await db.CvFormations.FirstOrDefaultAsync(f => f.Id == existingId && f.CandidateProfileId == candidateProfileId, cancellationToken);
                    if (existing is not null) return (existing, false);
                }

                var maxOrder = await db.CvFormations.Where(f => f.CandidateProfileId == candidateProfileId)
                    .Select(f => (int?)f.DisplayOrder).MaxAsync(cancellationToken) ?? -1;
                var created = new CvFormation { CandidateProfileId = candidateProfileId, DisplayOrder = maxOrder + 1 };
                db.CvFormations.Add(created);
                return (created, true);
            },
            entity =>
            {
                entity.Periode = input.Periode;
                entity.Etablissement = input.Etablissement;
                entity.DiplomeCertificatOuNiveau = input.DiplomeCertificatOuNiveau;
                entity.DomaineEtudes = input.DomaineEtudes;
                entity.UpdatedAt = DateTimeOffset.UtcNow;
            },
            entity => entity.Id,
            cancellationToken);

    public async Task DeleteFormationAsync(int candidateProfileId, int formationId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.CvFormations.FirstOrDefaultAsync(f => f.Id == formationId && f.CandidateProfileId == candidateProfileId, cancellationToken);
        if (entity is null) return; // Déjà supprimée (ou n'appartient pas à ce candidat) — suppression idempotente.
        db.CvFormations.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task SaveCompetencesEtudesAsync(int candidateProfileId, CvCompetencesEtudes input, CancellationToken cancellationToken = default) =>
        MutateWithRetryAsync<CvCompetencesEtudes>(
            async db =>
            {
                var existing = await db.CvCompetencesEtudes.FirstOrDefaultAsync(c => c.CandidateProfileId == candidateProfileId, cancellationToken);
                if (existing is not null) return (existing, false);
                var created = new CvCompetencesEtudes { CandidateProfileId = candidateProfileId };
                db.CvCompetencesEtudes.Add(created);
                return (created, true);
            },
            entity =>
            {
                entity.SpecialitePrincipale = input.SpecialitePrincipale;
                entity.CompetencesTechniques = input.CompetencesTechniques;
                entity.ConnaissancesTheoriques = input.ConnaissancesTheoriques;
                entity.LanguesMaitrisees = input.LanguesMaitrisees;
                entity.OutilsLogicielsMethodes = input.OutilsLogicielsMethodes;
                entity.UpdatedAt = DateTimeOffset.UtcNow;
            },
            cancellationToken);

    public Task<int> SaveExperienceAsync(int candidateProfileId, int? id, CvExperience input, CancellationToken cancellationToken = default) =>
        MutateWithRetryAsync(
            async db =>
            {
                if (id is int existingId)
                {
                    var existing = await db.CvExperiences.FirstOrDefaultAsync(x => x.Id == existingId && x.CandidateProfileId == candidateProfileId, cancellationToken);
                    if (existing is not null) return (existing, false);
                }

                var maxOrder = await db.CvExperiences.Where(x => x.CandidateProfileId == candidateProfileId)
                    .Select(x => (int?)x.DisplayOrder).MaxAsync(cancellationToken) ?? -1;
                var created = new CvExperience { CandidateProfileId = candidateProfileId, DisplayOrder = maxOrder + 1 };
                db.CvExperiences.Add(created);
                return (created, true);
            },
            entity =>
            {
                entity.Periode = input.Periode;
                entity.EntrepriseOrganisationOuStage = input.EntrepriseOrganisationOuStage;
                entity.FonctionOuActiviteExercee = input.FonctionOuActiviteExercee;
                entity.CompetencesDeveloppees = input.CompetencesDeveloppees;
                entity.UpdatedAt = DateTimeOffset.UtcNow;
            },
            entity => entity.Id,
            cancellationToken);

    public async Task DeleteExperienceAsync(int candidateProfileId, int experienceId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.CvExperiences.FirstOrDefaultAsync(x => x.Id == experienceId && x.CandidateProfileId == candidateProfileId, cancellationToken);
        if (entity is null) return;
        db.CvExperiences.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task SaveCaracteristiquesPersonnellesAsync(int candidateProfileId, CvCaracteristiquesPersonnelles input, CancellationToken cancellationToken = default) =>
        MutateWithRetryAsync<CvCaracteristiquesPersonnelles>(
            async db =>
            {
                var existing = await db.CvCaracteristiquesPersonnelles.FirstOrDefaultAsync(c => c.CandidateProfileId == candidateProfileId, cancellationToken);
                if (existing is not null) return (existing, false);
                var created = new CvCaracteristiquesPersonnelles { CandidateProfileId = candidateProfileId };
                db.CvCaracteristiquesPersonnelles.Add(created);
                return (created, true);
            },
            entity =>
            {
                entity.QualitesPersonnelles = input.QualitesPersonnelles;
                entity.AptitudesProfessionnelles = input.AptitudesProfessionnelles;
                entity.AttitudesRelationnelles = input.AttitudesRelationnelles;
                entity.CapaciteSousPression = input.CapaciteSousPression;
                entity.DisponibiliteMobilite = input.DisponibiliteMobilite;
                entity.UpdatedAt = DateTimeOffset.UtcNow;
            },
            cancellationToken);

    public Task SaveLoisirsAsync(int candidateProfileId, CvLoisirs input, CancellationToken cancellationToken = default) =>
        MutateWithRetryAsync<CvLoisirs>(
            async db =>
            {
                var existing = await db.CvLoisirs.FirstOrDefaultAsync(l => l.CandidateProfileId == candidateProfileId, cancellationToken);
                if (existing is not null) return (existing, false);
                var created = new CvLoisirs { CandidateProfileId = candidateProfileId };
                db.CvLoisirs.Add(created);
                return (created, true);
            },
            entity =>
            {
                entity.LoisirsPreferes = input.LoisirsPreferes;
                entity.ActivitesSportivesCulturelles = input.ActivitesSportivesCulturelles;
                entity.EngagementsAssociatifs = input.EngagementsAssociatifs;
                entity.AutresCentresInteret = input.AutresCentresInteret;
                entity.UpdatedAt = DateTimeOffset.UtcNow;
            },
            cancellationToken);

    public Task<int> SaveReferenceAsync(int candidateProfileId, int? id, CvReference input, CancellationToken cancellationToken = default) =>
        MutateWithRetryAsync(
            async db =>
            {
                if (id is int existingId)
                {
                    var existing = await db.CvReferences.FirstOrDefaultAsync(r => r.Id == existingId && r.CandidateProfileId == candidateProfileId, cancellationToken);
                    if (existing is not null) return (existing, false);
                }

                var maxOrder = await db.CvReferences.Where(r => r.CandidateProfileId == candidateProfileId)
                    .Select(r => (int?)r.DisplayOrder).MaxAsync(cancellationToken) ?? -1;
                var created = new CvReference { CandidateProfileId = candidateProfileId, DisplayOrder = maxOrder + 1 };
                db.CvReferences.Add(created);
                return (created, true);
            },
            entity =>
            {
                entity.NomPrenom = input.NomPrenom;
                entity.Fonction = input.Fonction;
                entity.EntrepriseOrganisation = input.EntrepriseOrganisation;
                entity.TelephoneOuEmail = input.TelephoneOuEmail;
                entity.LienAvecPostulant = input.LienAvecPostulant;
                entity.UpdatedAt = DateTimeOffset.UtcNow;
            },
            entity => entity.Id,
            cancellationToken);

    public async Task DeleteReferenceAsync(int candidateProfileId, int referenceId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.CvReferences.FirstOrDefaultAsync(r => r.Id == referenceId && r.CandidateProfileId == candidateProfileId, cancellationToken);
        if (entity is null) return;
        db.CvReferences.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task SaveDeclarationAsync(int candidateProfileId, CvDeclaration input, CancellationToken cancellationToken = default) =>
        MutateWithRetryAsync<CvDeclaration>(
            async db =>
            {
                var existing = await db.CvDeclarations.FirstOrDefaultAsync(d => d.CandidateProfileId == candidateProfileId, cancellationToken);
                if (existing is not null) return (existing, false);
                var created = new CvDeclaration { CandidateProfileId = candidateProfileId };
                db.CvDeclarations.Add(created);
                return (created, true);
            },
            entity =>
            {
                entity.CertificationExactitude = input.CertificationExactitude;
                entity.ConsentementConsultation = input.ConsentementConsultation;
                entity.Date = input.Date;
                entity.NomSignataire = input.NomSignataire;
                entity.UpdatedAt = DateTimeOffset.UtcNow;
            },
            cancellationToken);

    /// <summary>
    /// Boucle de retry générique partagée par toutes les sections du CV : <paramref name="loadOrCreate"/>
    /// est écrit par l'appelant avec des lambdas LINQ CONCRÈTES (types réels, jamais une contrainte
    /// d'interface générique) pour éviter tout risque d'échec de traduction LINQ-vers-SQL — ce générique
    /// n'orchestre que la boucle tentative/sauvegarde/repli, il ne construit aucune expression de requête
    /// lui-même. Même stratégie que <see cref="MutateCriteriaAsync"/>/<see cref="MutateAnswerAsync"/> :
    /// relecture + réapplication complète de la section sur conflit de concurrence (sûr ici car chaque appel
    /// écrit toujours la section entière qu'il possède, jamais une mutation partielle d'un état inconnu).
    /// </summary>
    private Task MutateWithRetryAsync<TEntity>(
        Func<ProfilCandidatDbContext, Task<(TEntity Entity, bool IsNew)>> loadOrCreate,
        Action<TEntity> applyChanges,
        CancellationToken cancellationToken)
        where TEntity : class =>
        MutateWithRetryAsync(loadOrCreate, applyChanges, static _ => true, cancellationToken);

    private async Task<TResult> MutateWithRetryAsync<TEntity, TResult>(
        Func<ProfilCandidatDbContext, Task<(TEntity Entity, bool IsNew)>> loadOrCreate,
        Action<TEntity> applyChanges,
        Func<TEntity, TResult> selectResult,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        const int maxAttempts = 30;
        var random = Random.Shared;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var (entity, isNew) = await loadOrCreate(db);
            applyChanges(entity);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return selectResult(entity);
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
            $"Impossible d'enregistrer cette section du CV ({typeof(TEntity).Name}) après {maxAttempts} tentatives (conflits de concurrence répétés).");
    }

    public async Task<CandidateCompatibilityCriteriaView?> GetCompatibilityCriteriaAsync(int candidateProfileId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

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
