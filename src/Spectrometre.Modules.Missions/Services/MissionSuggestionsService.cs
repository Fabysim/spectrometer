using Microsoft.EntityFrameworkCore;
using Spectrometre.Modules.JeunesPrestataires.Data;
using Spectrometre.Modules.JeunesPrestataires.Entities;
using Spectrometre.Modules.JeunesPrestataires.Services;
using Spectrometre.Modules.Missions.Catalog;
using Spectrometre.Modules.Missions.Data;
using Spectrometre.Modules.Missions.Entities;

namespace Spectrometre.Modules.Missions.Services;

public interface IMissionSuggestionsService
{
    /// <summary>
    /// Missions <see cref="MissionStatut.Disponible"/> dont la catégorie correspond aux
    /// préférences <c>p2.s12.missions_priorite</c>. Liste vide si aucune préférence déclarée,
    /// si le profil est <see cref="ProfilAccompagnement.Autonome"/> (même règle que le
    /// masquage menu de « Missions disponibles »), ou s’il n’y a pas de mission correspondante.
    /// <c>null</c> si pas de profil jeune.
    /// </summary>
    Task<IReadOnlyList<MissionResumeView>?> GetRecommandeesAsync(
        string jeuneUserId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Couche 1 « Mes opportunités » : suggestions de missions personnalisées uniquement
/// (pas de formations / stages / ateliers — hors catalogue). Lecture
/// <see cref="JeunesPrestatairesDbContext"/> via factory, comme
/// <see cref="MesAstucesRecommandationService"/>.
/// Réservé à <see cref="ProfilAccompagnement.SansExperience"/> : les missions disponibles
/// sont déjà retirées du menu pour <see cref="ProfilAccompagnement.Autonome"/>.
/// </summary>
public sealed class MissionSuggestionsService(
    IJeuneProfileService jeuneProfileService,
    IDbContextFactory<MissionsDbContext> missionsDbFactory,
    IDbContextFactory<JeunesPrestatairesDbContext> jeunesDbFactory) : IMissionSuggestionsService
{
    public async Task<IReadOnlyList<MissionResumeView>?> GetRecommandeesAsync(
        string jeuneUserId,
        CancellationToken cancellationToken = default)
    {
        var jeune = await jeuneProfileService.TryGetByUserIdAsync(jeuneUserId, cancellationToken);
        if (jeune is null)
            return null;

        if (jeune.ProfilAccompagnement != ProfilAccompagnement.SansExperience)
            return [];

        await using var jeunesDb = await jeunesDbFactory.CreateDbContextAsync(cancellationToken);
        var textValue = await jeunesDb.AutoObservationReponses.AsNoTracking()
            .Where(r => r.JeuneProfileId == jeune.Id && r.QuestionKey == MissionPreferenceCategorieMap.QuestionKey)
            .Select(r => r.TextValue)
            .FirstOrDefaultAsync(cancellationToken);

        var categories = MissionPreferenceCategorieMap.CategoriesDepuisTextValue(textValue);
        if (categories.Count == 0)
            return [];

        var categoryList = categories.ToList();
        await using var missionsDb = await missionsDbFactory.CreateDbContextAsync(cancellationToken);
        return await missionsDb.Missions.AsNoTracking()
            .Where(m => m.Statut == MissionStatut.Disponible && categoryList.Contains(m.Categorie))
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new MissionResumeView(
                m.Id,
                m.Titre,
                m.Categorie,
                m.Lieu,
                m.Statut,
                m.Difficulte,
                m.NiveauEncadrement,
                m.RemunerationMontant,
                m.PresenceEscaliers,
                m.PresenceAnimaux,
                m.PortDeCharge,
                m.AccesDifficile,
                m.RisqueParticulier,
                m.CreatedAt,
                null,
                null,
                null))
            .ToListAsync(cancellationToken);
    }
}
