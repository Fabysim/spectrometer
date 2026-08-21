using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Notifications;
using Spectrometre.Modules.Coaching.Entities;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Data;
using Spectrometre.Modules.JeunesPrestataires.Entities;

namespace Spectrometre.Modules.JeunesPrestataires.Services;

public sealed class ConsentementParentalService(
    IDbContextFactory<JeunesPrestatairesDbContext> dbFactory,
    ICoachingService coachingService,
    IJeuneProfileService jeuneProfileService,
    INotificationService notificationService) : IConsentementParentalService
{
    public async Task<ConsentementParentalView> GetAsync(int jeuneProfileId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.ConsentementsParentaux
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.JeuneProfileId == jeuneProfileId, cancellationToken);

        entity ??= new ConsentementParental { JeuneProfileId = jeuneProfileId };
        return new ConsentementParentalView(entity, entity.ValideLe is not null);
    }

    public async Task SaveBrouillonAsync(
        int jeuneProfileId,
        ConsentementParentalFormModel form,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.ConsentementsParentaux
            .FirstOrDefaultAsync(c => c.JeuneProfileId == jeuneProfileId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (entity is null)
        {
            entity = new ConsentementParental
            {
                JeuneProfileId = jeuneProfileId,
                CreatedAt = now,
            };
            db.ConsentementsParentaux.Add(entity);
        }

        form.ApplyTo(entity);
        entity.ValideLe = null;
        entity.NomJeuneConfirmation = null;
        entity.NomParent1Confirmation = null;
        entity.NomParent2Confirmation = null;
        entity.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReprendreEditionAsync(int jeuneProfileId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.ConsentementsParentaux
            .FirstOrDefaultAsync(c => c.JeuneProfileId == jeuneProfileId, cancellationToken);
        if (entity is null)
            return;

        entity.ValideLe = null;
        entity.NomJeuneConfirmation = null;
        entity.NomParent1Confirmation = null;
        entity.NomParent2Confirmation = null;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ConsentementConfirmationResult> ConfirmerAsync(
        int jeuneProfileId,
        string nomJeune,
        string nomParent1,
        string? nomParent2,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.ConsentementsParentaux
            .FirstOrDefaultAsync(c => c.JeuneProfileId == jeuneProfileId, cancellationToken);

        if (entity is null)
            return new ConsentementConfirmationResult(false, [ConsentementChamps.Parent1Nom]);

        var manquants = CollecterChampsObligatoiresManquants(entity, nomJeune, nomParent1, nomParent2);
        if (manquants.Count > 0)
            return new ConsentementConfirmationResult(false, manquants);

        // Uniquement la première validation : ReprendreEdition / brouillon remettent ValideLe à null.
        var premiereConfirmation = entity.ValideLe is null;
        var now = DateTimeOffset.UtcNow;
        entity.NomJeuneConfirmation = nomJeune.Trim();
        entity.NomParent1Confirmation = nomParent1.Trim();
        entity.NomParent2Confirmation = string.IsNullOrWhiteSpace(nomParent2) ? null : nomParent2.Trim();
        entity.ValideLe = now;
        entity.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);

        if (premiereConfirmation)
            await NotifierCoachConsentementConfirmeAsync(jeuneProfileId, cancellationToken);

        return new ConsentementConfirmationResult(true, []);
    }

    private async Task NotifierCoachConsentementConfirmeAsync(
        int jeuneProfileId,
        CancellationToken cancellationToken)
    {
        var jeune = await jeuneProfileService.TryGetByIdAsync(jeuneProfileId, cancellationToken);
        if (jeune is null || !jeuneProfileService.EstMineur(jeune.DateNaissance))
            return;

        var liens = await coachingService.GetLiensPourSuiviAsync(jeune.UserId, cancellationToken);
        var coachId = liens.FirstOrDefault(l => l.Statut == LienCoachingStatut.Actif)?.CoachUserId;
        if (coachId is null)
            return;

        var jeuneNom = $"{jeune.Prenoms} {jeune.Nom}".Trim();
        await notificationService.CreerAsync(
            coachId,
            "Consentement parental confirmé",
            $"{jeuneNom} a un consentement parental validé.",
            $"/coach/suivis/{jeune.UserId}/apercu",
            "JeunesPrestataires.ConsentementConfirme",
            cancellationToken);
    }

    public async Task<bool> EstConsentementValideAsync(int jeuneProfileId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var jeune = await db.JeuneProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == jeuneProfileId, cancellationToken);
        if (jeune is null)
            return false;

        if (!JeuneProfileService.EstMineurStatique(jeune.DateNaissance))
            return true;

        return await db.ConsentementsParentaux
            .AsNoTracking()
            .AnyAsync(c => c.JeuneProfileId == jeuneProfileId && c.ValideLe != null, cancellationToken);
    }

    public async Task<ConsentementParentalCoachView?> TryGetPourCoachAsync(
        string coachUserId,
        int jeuneProfileId,
        CancellationToken cancellationToken = default)
    {
        var jeune = await jeuneProfileService.TryGetByIdAsync(jeuneProfileId, cancellationToken);
        if (jeune is null)
            return null;

        var autorise = await coachingService.GetSuiviUserIdSiAutoriseAsync(
            jeune.UserId, coachUserId, cancellationToken);
        if (autorise is null)
            return null;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.ConsentementsParentaux.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.JeuneProfileId == jeuneProfileId && c.ValideLe != null,
                cancellationToken);
        if (entity is null || entity.ValideLe is not { } valideLe || string.IsNullOrWhiteSpace(entity.Parent1Nom))
            return null;

        RepresentantLegalCoachView? parent2 = null;
        if (!string.IsNullOrWhiteSpace(entity.Parent2Nom))
        {
            parent2 = new RepresentantLegalCoachView(
                entity.Parent2Nom.Trim(),
                TrimOrNull(entity.Parent2Lien),
                TrimOrNull(entity.Parent2Telephone),
                TrimOrNull(entity.Parent2Email));
        }

        return new ConsentementParentalCoachView(
            new RepresentantLegalCoachView(
                entity.Parent1Nom.Trim(),
                TrimOrNull(entity.Parent1Lien),
                TrimOrNull(entity.Parent1Telephone),
                TrimOrNull(entity.Parent1Email)),
            parent2,
            valideLe,
            entity.EngagementScolariteSanteEquilibre,
            entity.EngagementInformerContraintes,
            entity.EngagementEncouragerCharte,
            entity.EngagementSignalerMissionInadaptee,
            entity.EngagementCollaborerCoach);
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    internal static List<string> CollecterChampsObligatoiresManquants(
        ConsentementParental entity,
        string nomJeune,
        string nomParent1,
        string? nomParent2)
    {
        var manquants = new List<string>();

        if (string.IsNullOrWhiteSpace(entity.Parent1Nom))
            manquants.Add(ConsentementChamps.Parent1Nom);
        if (string.IsNullOrWhiteSpace(entity.Parent1Lien))
            manquants.Add(ConsentementChamps.Parent1Lien);
        if (string.IsNullOrWhiteSpace(entity.Parent1Adresse))
            manquants.Add(ConsentementChamps.Parent1Adresse);
        if (string.IsNullOrWhiteSpace(entity.Parent1Telephone))
            manquants.Add(ConsentementChamps.Parent1Telephone);
        if (string.IsNullOrWhiteSpace(entity.Parent1Email))
            manquants.Add(ConsentementChamps.Parent1Email);

        if (!entity.AutorisationMissions)
            manquants.Add(ConsentementChamps.AutorisationMissions);
        if (!entity.AutorisationRevenus)
            manquants.Add(ConsentementChamps.AutorisationRevenus);
        if (entity.PartParascolairePourcent is null)
            manquants.Add(ConsentementChamps.PartParascolairePourcent);
        if (entity.PartArgentDePochePourcent is null)
            manquants.Add(ConsentementChamps.PartArgentDePochePourcent);
        if (!entity.AutorisationDonneesEtImage)
            manquants.Add(ConsentementChamps.AutorisationDonneesEtImage);

        if (!entity.EngagementScolariteSanteEquilibre)
            manquants.Add(ConsentementChamps.EngagementScolariteSanteEquilibre);
        if (!entity.EngagementInformerContraintes)
            manquants.Add(ConsentementChamps.EngagementInformerContraintes);
        if (!entity.EngagementEncouragerCharte)
            manquants.Add(ConsentementChamps.EngagementEncouragerCharte);
        if (!entity.EngagementSignalerMissionInadaptee)
            manquants.Add(ConsentementChamps.EngagementSignalerMissionInadaptee);
        if (!entity.EngagementCollaborerCoach)
            manquants.Add(ConsentementChamps.EngagementCollaborerCoach);

        if (string.IsNullOrWhiteSpace(nomJeune))
            manquants.Add(ConsentementChamps.NomJeuneConfirmation);
        if (string.IsNullOrWhiteSpace(nomParent1))
            manquants.Add(ConsentementChamps.NomParent1Confirmation);
        if (!string.IsNullOrWhiteSpace(entity.Parent2Nom) && string.IsNullOrWhiteSpace(nomParent2))
            manquants.Add(ConsentementChamps.NomParent2Confirmation);

        return manquants;
    }
}
