using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Notifications;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.ProfilEntreprise.Services;
using Spectrometre.Modules.SuiviEmployes.Data;
using Spectrometre.Modules.SuiviEmployes.Entities;

namespace Spectrometre.Modules.SuiviEmployes.Services;

/// <summary>
/// Logique métier SuiviEmployes. Critères de poste lus en lecture seule via ProfilEntreprise.
/// Après <see cref="ValiderProfilInitialAsync"/>, la liste des critères est figée sur les
/// <c>CritereId</c> déjà scorés (comportement mvp — pas de rétro-application si le poste change).
/// </summary>
public sealed class SuiviEmployesService(
    IDbContextFactory<SuiviEmployesDbContext> suiviFactory,
    ITenantContext tenantContext,
    CoreDbContext coreDb,
    UserManager<ApplicationUser> userManager,
    IPosteService posteService,
    IAnalyseEmployeIaService analyseIa,
    INotificationService notificationService) : ISuiviEmployesService
{
    /// <summary>Seuil sous lequel une note d'objectif compte pour la série critique.</summary>
    public const int SeuilCritiqueNote = 40;

    /// <summary>Nombre de notes consécutives sous seuil pour lever l'indicateur.</summary>
    public const int SeuilCritiqueConsecutive = 3;

    private async Task<SuiviEmployesDbContext> CreateSuiviDbAsync(CancellationToken ct)
    {
        var db = await suiviFactory.CreateDbContextAsync(ct);
        db.TenantSchema = tenantContext.SchemaName
            ?? throw new InvalidOperationException("Aucune entreprise active.");
        return db;
    }

    public async Task<EmployeContexte?> GetContexteAsync(int userCompanyLinkId, CancellationToken cancellationToken = default)
    {
        var link = await coreDb.UserCompanyLinks.AsNoTracking()
            .Include(l => l.Company)
            .FirstOrDefaultAsync(l => l.Id == userCompanyLinkId, cancellationToken);
        if (link?.Company is null)
            return null;

        var user = await userManager.FindByIdAsync(link.UserId);
        string? posteTitre = null;
        if (link.PosteId is int posteId)
        {
            tenantContext.SetActiveCompany(link.CompanyId, link.Company.SchemaName);
            var postes = await posteService.GetPostesAsync(cancellationToken);
            posteTitre = postes.FirstOrDefault(p => p.Id == posteId)?.Titre;
        }

        var seuil = false;
        if (!string.IsNullOrEmpty(link.Company.SchemaName))
        {
            tenantContext.SetActiveCompany(link.CompanyId, link.Company.SchemaName);
            await using var db = await CreateSuiviDbAsync(cancellationToken);
            seuil = await db.EvaluationsObjectifs.AsNoTracking()
                .AnyAsync(e => e.UserCompanyLinkId == userCompanyLinkId && e.SeuilCritiqueAtteint, cancellationToken);
        }

        return new EmployeContexte(
            link.Id,
            link.UserId,
            user?.Email ?? link.UserId,
            link.CompanyId,
            link.Company.Name,
            link.Company.SchemaName,
            link.PosteId,
            posteTitre,
            seuil);
    }

    public async Task<IReadOnlyList<EmployeRattachementOption>> ListRattachementsEmployeAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var links = await coreDb.UserCompanyLinks.AsNoTracking()
            .Include(l => l.Company)
            .Where(l => l.UserId == userId && l.Role == CompanyRole.Employe)
            .OrderBy(l => l.Company!.Name)
            .ToListAsync(cancellationToken);

        var result = new List<EmployeRattachementOption>();
        foreach (var link in links)
        {
            if (link.Company is null)
                continue;

            string? posteTitre = null;
            if (link.PosteId is int posteId)
            {
                tenantContext.SetActiveCompany(link.CompanyId, link.Company.SchemaName);
                var postes = await posteService.GetPostesAsync(cancellationToken);
                posteTitre = postes.FirstOrDefault(p => p.Id == posteId)?.Titre;
            }

            result.Add(new EmployeRattachementOption(link.Id, link.CompanyId, link.Company.Name, posteTitre));
        }

        return result;
    }

    public async Task<ProfilProfessionnelPageData?> GetProfilProfessionnelAsync(
        int userCompanyLinkId,
        CancellationToken cancellationToken = default)
    {
        var contexte = await GetContexteAsync(userCompanyLinkId, cancellationToken);
        if (contexte is null || contexte.PosteId is null)
            return contexte is null
                ? null
                : new ProfilProfessionnelPageData(contexte, false, [], []);

        tenantContext.SetActiveCompany(contexte.CompanyId, contexte.SchemaName);
        await using var db = await CreateSuiviDbAsync(cancellationToken);

        var validation = await db.ValidationsSocioProEmploye.AsNoTracking()
            .FirstOrDefaultAsync(
                v => v.UserCompanyLinkId == userCompanyLinkId && v.PosteId == contexte.PosteId.Value,
                cancellationToken);
        var validationFaite = validation is not null;

        var scores = await db.EvaluationsEmploye.AsNoTracking()
            .Where(e => e.UserCompanyLinkId == userCompanyLinkId && e.PosteId == contexte.PosteId.Value)
            .OrderBy(e => e.EvaluationDate)
            .ThenBy(e => e.DaySequence)
            .ThenBy(e => e.CritereId)
            .ToListAsync(cancellationToken);

        IReadOnlyList<CritereProfilView> criteres;
        if (validationFaite)
        {
            // Liste figée : CritereId déjà scorés (pas le catalogue live du poste).
            var ids = scores.Select(s => s.CritereId).Distinct().ToList();
            var live = await posteService.GetCriteresAsync(contexte.PosteId.Value, cancellationToken);
            var byId = live.ToDictionary(c => c.Id);
            criteres = ids
                .Select(id => byId.TryGetValue(id, out var c)
                    ? new CritereProfilView(c.Id, c.Categorie, c.Libelle)
                    : new CritereProfilView(id, "—", $"Critère #{id}"))
                .OrderBy(c => c.Categorie)
                .ThenBy(c => c.Libelle)
                .ToList();
        }
        else
        {
            var live = await posteService.GetCriteresAsync(contexte.PosteId.Value, cancellationToken);
            criteres = live
                .OrderBy(c => c.OrdreAffichage)
                .ThenBy(c => c.Categorie)
                .ThenBy(c => c.Libelle)
                .Select(c => new CritereProfilView(c.Id, c.Categorie, c.Libelle))
                .ToList();
        }

        var blocs = scores
            .GroupBy(s => new { s.EvaluationDate, s.DaySequence, s.IsClosed })
            .OrderBy(g => g.Key.EvaluationDate)
            .ThenBy(g => g.Key.DaySequence)
            .Select(g => new BlocEvaluationView(
                g.Key.EvaluationDate,
                g.Key.DaySequence,
                g.Key.IsClosed,
                g.Select(s => new ScoreBlocView(s.CritereId, s.ScoreActuel, s.ScoreSouhaite)).ToList()))
            .ToList();

        return new ProfilProfessionnelPageData(contexte, validationFaite, criteres, blocs);
    }

    public async Task ValiderProfilInitialAsync(
        int userCompanyLinkId,
        IReadOnlyList<ScoreSaisieDto> scores,
        CancellationToken cancellationToken = default)
    {
        var contexte = await RequireContexteAvecPosteAsync(userCompanyLinkId, cancellationToken);
        tenantContext.SetActiveCompany(contexte.CompanyId, contexte.SchemaName);
        await using var db = await CreateSuiviDbAsync(cancellationToken);

        var exists = await db.ValidationsSocioProEmploye
            .AnyAsync(v => v.UserCompanyLinkId == userCompanyLinkId && v.PosteId == contexte.PosteId!.Value, cancellationToken);
        if (exists)
            throw new InvalidOperationException("Le profil initial est déjà validé.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        const int seq = 1;

        db.ValidationsSocioProEmploye.Add(new ValidationSocioProEmploye
        {
            UserCompanyLinkId = userCompanyLinkId,
            PosteId = contexte.PosteId!.Value,
            ValidatedAtUtc = DateTimeOffset.UtcNow,
        });

        foreach (var s in scores)
        {
            db.EvaluationsEmploye.Add(new EvaluationEmploye
            {
                UserCompanyLinkId = userCompanyLinkId,
                PosteId = contexte.PosteId.Value,
                CritereId = s.CritereId,
                ScoreActuel = ClampScore(s.ScoreActuel),
                ScoreSouhaite = ClampScore(s.ScoreSouhaite),
                EvaluationDate = today,
                DaySequence = seq,
                IsClosed = true,
            });
        }

        if (!await db.EvaluationsEmployeCloturees.AnyAsync(
                c => c.UserCompanyLinkId == userCompanyLinkId
                     && c.PosteId == contexte.PosteId.Value
                     && c.EvaluationDate == today,
                cancellationToken))
        {
            db.EvaluationsEmployeCloturees.Add(new EvaluationEmployeCloturee
            {
                UserCompanyLinkId = userCompanyLinkId,
                PosteId = contexte.PosteId.Value,
                EvaluationDate = today,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetNextDaySequenceAsync(
        int userCompanyLinkId,
        DateOnly evaluationDate,
        CancellationToken cancellationToken = default)
    {
        var contexte = await RequireContexteAvecPosteAsync(userCompanyLinkId, cancellationToken);
        tenantContext.SetActiveCompany(contexte.CompanyId, contexte.SchemaName);
        await using var db = await CreateSuiviDbAsync(cancellationToken);

        var max = await db.EvaluationsEmploye.AsNoTracking()
            .Where(e => e.UserCompanyLinkId == userCompanyLinkId
                        && e.PosteId == contexte.PosteId!.Value
                        && e.EvaluationDate == evaluationDate)
            .Select(e => (int?)e.DaySequence)
            .MaxAsync(cancellationToken);

        return (max ?? 0) + 1;
    }

    public async Task SaveScoresBlocAsync(
        int userCompanyLinkId,
        DateOnly evaluationDate,
        int daySequence,
        IReadOnlyList<ScoreSaisieDto> scores,
        CancellationToken cancellationToken = default)
    {
        var contexte = await RequireContexteAvecPosteAsync(userCompanyLinkId, cancellationToken);
        tenantContext.SetActiveCompany(contexte.CompanyId, contexte.SchemaName);
        await using var db = await CreateSuiviDbAsync(cancellationToken);

        var existing = await db.EvaluationsEmploye
            .Where(e => e.UserCompanyLinkId == userCompanyLinkId
                        && e.PosteId == contexte.PosteId!.Value
                        && e.EvaluationDate == evaluationDate
                        && e.DaySequence == daySequence)
            .ToListAsync(cancellationToken);

        if (existing.Any(e => e.IsClosed))
            throw new InvalidOperationException("Ce bloc d'évaluation est déjà clôturé.");

        foreach (var s in scores)
        {
            var row = existing.FirstOrDefault(e => e.CritereId == s.CritereId);
            if (row is null)
            {
                db.EvaluationsEmploye.Add(new EvaluationEmploye
                {
                    UserCompanyLinkId = userCompanyLinkId,
                    PosteId = contexte.PosteId!.Value,
                    CritereId = s.CritereId,
                    ScoreActuel = ClampScore(s.ScoreActuel),
                    ScoreSouhaite = ClampScore(s.ScoreSouhaite),
                    EvaluationDate = evaluationDate,
                    DaySequence = daySequence,
                    IsClosed = false,
                });
            }
            else
            {
                row.ScoreActuel = ClampScore(s.ScoreActuel);
                row.ScoreSouhaite = ClampScore(s.ScoreSouhaite);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task CloturerBlocAsync(
        int userCompanyLinkId,
        DateOnly evaluationDate,
        int daySequence,
        CancellationToken cancellationToken = default)
    {
        var contexte = await RequireContexteAvecPosteAsync(userCompanyLinkId, cancellationToken);
        tenantContext.SetActiveCompany(contexte.CompanyId, contexte.SchemaName);
        await using var db = await CreateSuiviDbAsync(cancellationToken);

        var rows = await db.EvaluationsEmploye
            .Where(e => e.UserCompanyLinkId == userCompanyLinkId
                        && e.PosteId == contexte.PosteId!.Value
                        && e.EvaluationDate == evaluationDate
                        && e.DaySequence == daySequence)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
            throw new InvalidOperationException("Aucun score à clôturer pour ce bloc.");

        foreach (var row in rows)
            row.IsClosed = true;

        if (!await db.EvaluationsEmployeCloturees.AnyAsync(
                c => c.UserCompanyLinkId == userCompanyLinkId
                     && c.PosteId == contexte.PosteId!.Value
                     && c.EvaluationDate == evaluationDate,
                cancellationToken))
        {
            db.EvaluationsEmployeCloturees.Add(new EvaluationEmployeCloturee
            {
                UserCompanyLinkId = userCompanyLinkId,
                PosteId = contexte.PosteId!.Value,
                EvaluationDate = evaluationDate,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<EvaluationObjectifsView> GetOrCreateEvaluationObjectifsCouranteAsync(
        int userCompanyLinkId,
        string? evaluateurUserId,
        CancellationToken cancellationToken = default)
    {
        var contexte = await GetContexteAsync(userCompanyLinkId, cancellationToken)
            ?? throw new InvalidOperationException("Employé introuvable.");
        tenantContext.SetActiveCompany(contexte.CompanyId, contexte.SchemaName);
        await using var db = await CreateSuiviDbAsync(cancellationToken);

        var courante = await db.EvaluationsObjectifs
            .Include(e => e.Objectifs)
            .FirstOrDefaultAsync(
                e => e.UserCompanyLinkId == userCompanyLinkId && !e.Archivee,
                cancellationToken);

        if (courante is null)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            courante = new EvaluationObjectifs
            {
                UserCompanyLinkId = userCompanyLinkId,
                DateDebut = today,
                DateFin = today.AddMonths(3),
                Archivee = false,
                EvaluateurUserId = evaluateurUserId,
            };
            db.EvaluationsObjectifs.Add(courante);
            await db.SaveChangesAsync(cancellationToken);
        }

        return ToEvaluationView(courante);
    }

    public async Task SaveObjectifsAsync(
        int userCompanyLinkId,
        IReadOnlyList<ObjectifSaisieDto> objectifs,
        bool archiver,
        string? evaluateurUserId,
        CancellationToken cancellationToken = default)
    {
        var contexte = await GetContexteAsync(userCompanyLinkId, cancellationToken)
            ?? throw new InvalidOperationException("Employé introuvable.");
        tenantContext.SetActiveCompany(contexte.CompanyId, contexte.SchemaName);
        await using var db = await CreateSuiviDbAsync(cancellationToken);

        var courante = await db.EvaluationsObjectifs
            .Include(e => e.Objectifs)
            .FirstOrDefaultAsync(
                e => e.UserCompanyLinkId == userCompanyLinkId && !e.Archivee,
                cancellationToken)
            ?? throw new InvalidOperationException("Aucune période d'évaluation ouverte.");

        if (!string.IsNullOrEmpty(evaluateurUserId))
            courante.EvaluateurUserId = evaluateurUserId;

        var keepIds = objectifs.Where(o => o.Id is > 0).Select(o => o.Id!.Value).ToHashSet();
        var toRemove = courante.Objectifs.Where(o => !keepIds.Contains(o.Id)).ToList();
        db.Objectifs.RemoveRange(toRemove);

        foreach (var dto in objectifs)
        {
            Objectif entity;
            if (dto.Id is int id && id > 0)
            {
                entity = courante.Objectifs.FirstOrDefault(o => o.Id == id)
                    ?? throw new InvalidOperationException($"Objectif #{id} introuvable.");
            }
            else
            {
                entity = new Objectif { EvaluationObjectifsId = courante.Id };
                db.Objectifs.Add(entity);
                courante.Objectifs.Add(entity);
            }

            entity.Date = dto.Date;
            entity.Titre = dto.Titre.Trim();
            entity.Moyens = string.IsNullOrWhiteSpace(dto.Moyens) ? null : dto.Moyens.Trim();
            entity.Atteinte = dto.Atteinte;
            entity.Observation = string.IsNullOrWhiteSpace(dto.Observation) ? null : dto.Observation.Trim();
            entity.Note = dto.Note is null ? null : ClampScore(dto.Note.Value);
        }

        await db.SaveChangesAsync(cancellationToken);

        // Recharger pour le calcul de seuil (notes à jour).
        await db.Entry(courante).Collection(e => e.Objectifs).LoadAsync(cancellationToken);
        var etaitCritique = courante.SeuilCritiqueAtteint;
        var devientCritique = DetecterSeuilCritique(courante.Objectifs);
        courante.SeuilCritiqueAtteint = devientCritique;

        if (archiver)
            courante.Archivee = true;

        await db.SaveChangesAsync(cancellationToken);

        // Notification propriétaire uniquement sur transition false → true (évite le spam).
        if (!etaitCritique && devientCritique)
            await NotifierProprietairesSeuilCritiqueAsync(contexte, cancellationToken);
    }

    private async Task NotifierProprietairesSeuilCritiqueAsync(
        EmployeContexte contexte,
        CancellationToken cancellationToken)
    {
        var proprietaires = await coreDb.UserCompanyLinks.AsNoTracking()
            .Where(l => l.CompanyId == contexte.CompanyId && l.Role == CompanyRole.Proprietaire)
            .Select(l => l.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var titre = "Seuil critique atteint";
        var message = $"Un seuil critique a été atteint pour {contexte.Email}.";
        var lien = $"/entreprise/employes/{contexte.UserCompanyLinkId}";

        foreach (var ownerId in proprietaires)
        {
            await notificationService.CreerAsync(
                ownerId,
                titre,
                message,
                lien,
                "SuiviEmployes.SeuilCritique",
                cancellationToken);
        }
    }

    public async Task<IReadOnlyList<EvaluationObjectifsView>> GetArchivesObjectifsAsync(
        int userCompanyLinkId,
        CancellationToken cancellationToken = default)
    {
        var contexte = await GetContexteAsync(userCompanyLinkId, cancellationToken);
        if (contexte is null)
            return [];

        tenantContext.SetActiveCompany(contexte.CompanyId, contexte.SchemaName);
        await using var db = await CreateSuiviDbAsync(cancellationToken);

        var archives = await db.EvaluationsObjectifs.AsNoTracking()
            .Include(e => e.Objectifs)
            .Where(e => e.UserCompanyLinkId == userCompanyLinkId && e.Archivee)
            .OrderByDescending(e => e.DateFin)
            .ThenByDescending(e => e.Id)
            .ToListAsync(cancellationToken);

        return archives.Select(ToEvaluationView).ToList();
    }

    public async Task<IReadOnlyList<PointCourbe>> GetEvolutionNotesObjectifsAsync(
        int userCompanyLinkId,
        CancellationToken cancellationToken = default)
    {
        var archives = await GetArchivesObjectifsAsync(userCompanyLinkId, cancellationToken);
        return archives
            .Select(a =>
            {
                var notes = a.Objectifs.Where(o => o.Note is not null).Select(o => (double)o.Note!).ToList();
                var avg = notes.Count == 0 ? 0 : notes.Average();
                var label = a.DateFin.ToString("dd/MM/yy", CultureInfo.InvariantCulture);
                return new PointCourbe(label, avg);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<SerieCritereCourbe>> GetEvolutionCriteresAsync(
        int userCompanyLinkId,
        CancellationToken cancellationToken = default)
    {
        var contexte = await GetContexteAsync(userCompanyLinkId, cancellationToken);
        if (contexte?.PosteId is null)
            return [];

        tenantContext.SetActiveCompany(contexte.CompanyId, contexte.SchemaName);
        await using var db = await CreateSuiviDbAsync(cancellationToken);

        var scores = await db.EvaluationsEmploye.AsNoTracking()
            .Where(e => e.UserCompanyLinkId == userCompanyLinkId
                        && e.PosteId == contexte.PosteId.Value
                        && e.IsClosed)
            .OrderBy(e => e.EvaluationDate)
            .ThenBy(e => e.DaySequence)
            .ToListAsync(cancellationToken);

        if (scores.Count == 0)
            return [];

        var live = await posteService.GetCriteresAsync(contexte.PosteId.Value, cancellationToken);
        var byId = live.ToDictionary(c => c.Id);

        return scores
            .GroupBy(s => s.CritereId)
            .Select(g =>
            {
                var id = g.Key;
                var cat = byId.TryGetValue(id, out var c) ? c.Categorie : "—";
                var lib = byId.TryGetValue(id, out var c2) ? c2.Libelle : $"Critère #{id}";
                var actuel = g.Select(s => new PointCourbe(FormatBlocLabel(s.EvaluationDate, s.DaySequence), s.ScoreActuel)).ToList();
                var souhaite = g.Select(s => new PointCourbe(FormatBlocLabel(s.EvaluationDate, s.DaySequence), s.ScoreSouhaite)).ToList();
                return new SerieCritereCourbe(id, cat, lib, actuel, souhaite);
            })
            .OrderBy(s => s.Categorie)
            .ThenBy(s => s.Libelle)
            .ToList();
    }

    public async Task<AnalyseIaEmployeView?> GetAnalyseIaAsync(
        int userCompanyLinkId,
        CancellationToken cancellationToken = default)
    {
        var contexte = await GetContexteAsync(userCompanyLinkId, cancellationToken);
        if (contexte is null)
            return null;

        tenantContext.SetActiveCompany(contexte.CompanyId, contexte.SchemaName);
        await using var db = await CreateSuiviDbAsync(cancellationToken);
        var entity = await db.AnalysesIaEmploye.AsNoTracking()
            .Where(a => a.UserCompanyLinkId == userCompanyLinkId && !a.EnCours)
            .OrderByDescending(a => a.GenereeLe)
            .FirstOrDefaultAsync(cancellationToken);
        return entity is null ? null : ToAnalyseView(entity);
    }

    public async Task<AnalyseIaEmployeView> GenererAnalyseEmployeAsync(
        int userCompanyLinkId,
        bool forcerRegeneration = false,
        CancellationToken cancellationToken = default)
    {
        var contexte = await GetContexteAsync(userCompanyLinkId, cancellationToken)
            ?? throw new InvalidOperationException("Employé introuvable.");

        tenantContext.SetActiveCompany(contexte.CompanyId, contexte.SchemaName);
        await using var db = await CreateSuiviDbAsync(cancellationToken);

        var scores = await db.EvaluationsEmploye.AsNoTracking()
            .Where(e => e.UserCompanyLinkId == userCompanyLinkId && e.IsClosed)
            .OrderBy(e => e.EvaluationDate).ThenBy(e => e.DaySequence)
            .ToListAsync(cancellationToken);
        var objectifs = await db.EvaluationsObjectifs.AsNoTracking()
            .Include(e => e.Objectifs)
            .Where(e => e.UserCompanyLinkId == userCompanyLinkId)
            .OrderBy(e => e.DateDebut)
            .ToListAsync(cancellationToken);

        var english = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "fr";
        var hash = CalculerHash(contexte, scores, objectifs, english);

        var existante = await db.AnalysesIaEmploye
            .Where(a => a.UserCompanyLinkId == userCompanyLinkId)
            .OrderByDescending(a => a.GenereeLe)
            .FirstOrDefaultAsync(cancellationToken);

        if (!forcerRegeneration
            && existante is not null
            && existante.DataHash == hash
            && !string.IsNullOrWhiteSpace(existante.AnalyseMarkdown))
        {
            return ToAnalyseView(existante);
        }

        string texte;
        var genereeParIa = false;
        string? avertissement = null;

        try
        {
            var systemPrompt = english
                ? "You are an HR analyst. Summarize employee progress trends (criteria + objectives): progression, stagnation or regression. Be concise, actionable, no raw dump of numbers."
                : "Tu es un analyste RH. Synthétise la tendance d'évolution de l'employé (critères + objectifs) : progression, stagnation ou régression. Sois concis et actionnable, sans lister les chiffres bruts.";
            var userPrompt = BuildPrompt(contexte, scores, objectifs, english);
            var (output, error) = await analyseIa.GenererTexteAsync(systemPrompt, userPrompt, cancellationToken);

            if (error is not null || string.IsNullOrWhiteSpace(output))
            {
                texte = BuildFallback(contexte, scores, objectifs, english);
                avertissement = error ?? (english ? "Empty AI response." : "Réponse IA vide.");
            }
            else
            {
                texte = output.Trim();
                genereeParIa = true;
            }
        }
        catch (Exception ex)
        {
            texte = BuildFallback(contexte, scores, objectifs, english);
            avertissement = ex.Message;
        }

        if (existante is null)
        {
            existante = new AnalyseIaEmploye { UserCompanyLinkId = userCompanyLinkId };
            db.AnalysesIaEmploye.Add(existante);
        }

        existante.AnalyseMarkdown = texte;
        existante.DataHash = hash;
        existante.GenereeParIa = genereeParIa;
        existante.GenereeLe = DateTimeOffset.UtcNow;
        existante.EnCours = false;
        await db.SaveChangesAsync(cancellationToken);

        return ToAnalyseView(existante, avertissement);
    }

    private async Task<EmployeContexte> RequireContexteAvecPosteAsync(int userCompanyLinkId, CancellationToken ct)
    {
        var contexte = await GetContexteAsync(userCompanyLinkId, ct)
            ?? throw new InvalidOperationException("Employé introuvable.");
        if (contexte.PosteId is null)
            throw new InvalidOperationException("Aucun poste n'est assigné à cet employé.");
        return contexte;
    }

    private static int ClampScore(int value) => Math.Clamp(value, 0, 100);

    private static bool DetecterSeuilCritique(IEnumerable<Objectif> objectifs)
    {
        var notes = objectifs
            .Where(o => o.Note is not null)
            .OrderBy(o => o.Date)
            .ThenBy(o => o.Id)
            .Select(o => o.Note!.Value)
            .ToList();

        var streak = 0;
        foreach (var note in notes)
        {
            if (note < SeuilCritiqueNote)
            {
                streak++;
                if (streak >= SeuilCritiqueConsecutive)
                    return true;
            }
            else
            {
                streak = 0;
            }
        }

        return false;
    }

    private static EvaluationObjectifsView ToEvaluationView(EvaluationObjectifs e) =>
        new(
            e.Id,
            e.DateDebut,
            e.DateFin,
            e.Archivee,
            e.SeuilCritiqueAtteint,
            e.Objectifs
                .OrderBy(o => o.Date)
                .ThenBy(o => o.Id)
                .Select(o => new ObjectifView(o.Id, o.Date, o.Titre, o.Moyens, o.Atteinte, o.Observation, o.Note))
                .ToList());

    private static AnalyseIaEmployeView ToAnalyseView(AnalyseIaEmploye e, string? avertissement = null) =>
        new(e.AnalyseMarkdown, e.GenereeLe, e.GenereeParIa, avertissement);

    private static string FormatBlocLabel(DateOnly date, int seq) =>
        seq > 1
            ? $"{date:dd/MM}#{seq}"
            : date.ToString("dd/MM", CultureInfo.InvariantCulture);

    private static string CalculerHash(
        EmployeContexte contexte,
        IReadOnlyList<EvaluationEmploye> scores,
        IReadOnlyList<EvaluationObjectifs> objectifs,
        bool english)
    {
        var sb = new StringBuilder();
        sb.Append(contexte.UserCompanyLinkId).Append('|').Append(contexte.PosteId)
            .Append('|').Append(english ? "en" : "fr");
        foreach (var s in scores)
        {
            sb.Append(';').Append(s.CritereId).Append(':').Append(s.EvaluationDate)
                .Append('#').Append(s.DaySequence)
                .Append('=').Append(s.ScoreActuel).Append('/').Append(s.ScoreSouhaite);
        }

        foreach (var ev in objectifs)
        {
            sb.Append('|').Append(ev.Id).Append(':').Append(ev.Archivee).Append(':').Append(ev.SeuilCritiqueAtteint);
            foreach (var o in ev.Objectifs.OrderBy(x => x.Id))
                sb.Append(';').Append(o.Id).Append(':').Append(o.Note).Append(':').Append((int)o.Atteinte);
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())));
    }

    private static string BuildPrompt(
        EmployeContexte contexte,
        IReadOnlyList<EvaluationEmploye> scores,
        IReadOnlyList<EvaluationObjectifs> objectifs,
        bool english)
    {
        var sb = new StringBuilder();
        sb.AppendLine(english
            ? $"Employee: {contexte.Email} / role: {contexte.PosteTitre ?? "n/a"}"
            : $"Employé : {contexte.Email} / poste : {contexte.PosteTitre ?? "n/a"}");
        sb.AppendLine(english ? "Closed criteria scores (date#seq:critere=actuel/souhaite):" : "Scores critères clôturés (date#seq:critere=actuel/souhaité) :");
        foreach (var s in scores)
            sb.AppendLine($"{s.EvaluationDate:yyyy-MM-dd}#{s.DaySequence}:{s.CritereId}={s.ScoreActuel}/{s.ScoreSouhaite}");
        sb.AppendLine(english ? "Objectives periods:" : "Périodes d'objectifs :");
        foreach (var ev in objectifs)
        {
            sb.AppendLine($"{ev.DateDebut:yyyy-MM-dd}->{ev.DateFin:yyyy-MM-dd} archived={ev.Archivee} critical={ev.SeuilCritiqueAtteint}");
            foreach (var o in ev.Objectifs)
                sb.AppendLine($"  - {o.Titre} note={o.Note} atteinte={o.Atteinte}");
        }

        return sb.ToString();
    }

    private static string BuildFallback(
        EmployeContexte contexte,
        IReadOnlyList<EvaluationEmploye> scores,
        IReadOnlyList<EvaluationObjectifs> objectifs,
        bool english)
    {
        var closedBlocks = scores.Select(s => (s.EvaluationDate, s.DaySequence)).Distinct().Count();
        var notes = objectifs.SelectMany(e => e.Objectifs).Where(o => o.Note is not null).Select(o => o.Note!.Value).ToList();
        var avg = notes.Count == 0 ? (double?)null : notes.Average();
        if (english)
        {
            return $"**Local summary (AI unavailable)** for {contexte.Email}. "
                   + $"{closedBlocks} closed evaluation block(s). "
                   + (avg is null ? "No objective scores yet." : $"Average objective score: {avg:0.#}/100. ")
                   + "Review criteria trends and consecutive low scores manually.";
        }

        return $"**Synthèse locale (IA indisponible)** pour {contexte.Email}. "
               + $"{closedBlocks} bloc(s) d'évaluation clôturé(s). "
               + (avg is null ? "Pas encore de notes d'objectifs. " : $"Note moyenne des objectifs : {avg:0.#}/100. ")
               + "Examinez manuellement les tendances des critères et les notes basses consécutives.";
    }
}
