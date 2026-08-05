using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Data;
using Spectrometre.Core.Recruitment;
using Spectrometre.Modules.PostesRecrutement.Data;

namespace Spectrometre.Host.Onboarding;

/// <summary>
/// Remplissage ponctuel de <see cref="IRecruitmentIndexService"/> (schéma <c>core</c>) à partir des postes
/// et candidatures déjà présents dans les schémas tenant AVANT l'introduction de cet index dans ce cycle —
/// sans ce backfill, les postes/candidatures créés lors de cycles précédents resteraient invisibles pour
/// <c>/candidat/postes</c> et le Vivier tant qu'aucun nouvel évènement (création, changement de statut) ne
/// les aurait fait ré-apparaître dans l'index.
/// </summary>
/// <remarks>
/// C'est la SEULE itération schéma par schéma restante dans l'application, et volontairement : elle ne
/// tourne qu'une fois au démarrage (pas sur le chemin de lecture d'un utilisateur), donc son coût ne pose
/// pas le problème de passage à l'échelle que l'index remplace. Idempotent (upsert), rejouable sans risque
/// à chaque redémarrage — pas besoin de suivre "déjà exécuté".
/// </remarks>
internal static class RecruitmentIndexBackfill
{
    public static async Task RunAsync(IServiceProvider services)
    {
        var coreDb = services.GetRequiredService<CoreDbContext>();
        var recruitmentIndex = services.GetRequiredService<IRecruitmentIndexService>();
        var postesDbFactory = services.GetRequiredService<IDbContextFactory<PostesRecrutementDbContext>>();

        var companies = await coreDb.Companies.AsNoTracking().ToListAsync();

        foreach (var company in companies)
        {
            PostesRecrutementDbContext db;
            try
            {
                db = await postesDbFactory.CreateDbContextAsync();
                db.TenantSchema = company.SchemaName;
                // Force l'exécution d'une requête pour détecter tout de suite un schéma pas encore
                // provisionné (table absente) plutôt que de laisser l'exception remonter plus loin.
                _ = await db.Postes.AsNoTracking().Select(p => p.Id).Take(1).ToListAsync();
            }
            catch (Npgsql.PostgresException)
            {
                // Module pas encore activé/provisionné pour cette entreprise — rien à réindexer.
                continue;
            }

            await using (db)
            {
                var postes = await db.Postes.AsNoTracking().ToListAsync();
                foreach (var poste in postes)
                {
                    await recruitmentIndex.UpsertPosteAsync(
                        company.Id, company.Name, poste.Id, poste.Titre, poste.Description, poste.Departement,
                        poste.Statut.ToString());
                }

                var candidatures = await db.Candidatures.AsNoTracking().ToListAsync();
                foreach (var candidature in candidatures)
                {
                    var posteTitre = postes.FirstOrDefault(p => p.Id == candidature.PosteId)?.Titre ?? "(poste supprimé)";
                    await recruitmentIndex.UpsertCandidatureAsync(
                        company.Id, candidature.PosteId, posteTitre, candidature.CandidateProfileId,
                        candidature.Statut.ToString(), scoreCompatibilite: null, tagsCles: []);
                }
            }
        }
    }
}
