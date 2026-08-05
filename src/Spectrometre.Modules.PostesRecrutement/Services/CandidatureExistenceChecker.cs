using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Data;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Recruitment;
using Spectrometre.Modules.PostesRecrutement.Data;

namespace Spectrometre.Modules.PostesRecrutement.Services;

/// <summary>
/// Implémentation réelle de <see cref="ICandidatureExistenceChecker"/> — voir le commentaire sur
/// l'interface pour l'inversion de dépendance. Enregistrée dans le conteneur DI depuis
/// <c>Spectrometre.Host.Program</c>, jamais depuis <c>Spectrometre.Modules.Compatibilite</c>.
/// </summary>
/// <remarks>
/// Interroge directement la table <c>Candidatures</c> du schéma tenant (source de vérité), PAS l'index
/// partagé <c>IRecruitmentIndexService</c> : ce dernier est une copie de lecture mise à jour de façon
/// événementielle (voir son commentaire), donc potentiellement en léger retard sur l'état réel — inadapté
/// à une décision d'autorisation, où une candidature qui existe déjà en base doit être immédiatement
/// reconnue.
/// </remarks>
public sealed class CandidatureExistenceChecker(
    IDbContextFactory<PostesRecrutementDbContext> dbFactory,
    CoreDbContext coreDb,
    IModuleRegistry moduleRegistry) : ICandidatureExistenceChecker
{
    public async Task<bool> ExisteCandidatureReelleAsync(int candidateProfileId, int companyId, CancellationToken cancellationToken = default)
    {
        // Comportement par défaut sûr : si PostesRecrutement n'est pas activé pour cette entreprise,
        // aucune candidature ne peut être prouvée — on refuse plutôt que de risquer un faux positif
        // (ex. un schéma tenant contenant encore d'anciennes données d'un module désactivé).
        if (!await moduleRegistry.IsActiveAsync(companyId, "PostesRecrutement", coreDb, cancellationToken))
            return false;

        var company = await coreDb.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
            return false;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        db.TenantSchema = company.SchemaName;

        try
        {
            return await db.Candidatures.AnyAsync(c => c.CandidateProfileId == candidateProfileId, cancellationToken);
        }
        catch (Npgsql.PostgresException)
        {
            // Schéma pas encore provisionné (ne devrait pas arriver si le module est actif, mais on ne
            // laisse jamais une exception d'infrastructure se traduire en autorisation implicite).
            return false;
        }
    }
}
