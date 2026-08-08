using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Directory;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Coaching.Data;
using Spectrometre.Modules.Coaching.Services;

namespace Spectrometre.Modules.Coaching;

/// <summary>
/// Contrairement aux autres modules, Coaching n'a PAS de <c>ModuleManifest</c>/activation propre : sa
/// disponibilité découle entièrement de deux prérequis déjà gérés ailleurs — Gestion du temps actif côté
/// personne suivie (<see cref="Spectrometre.Modules.GestionDuTemps.Services.IGestionDuTempsAccessService"/>)
/// et ProfilCoach actif côté coach (voir <c>ModuleActivationSubjectType.Coach</c>). Ajouter un 3e indicateur
/// d'activation sans signification propre (aucun sujet ne "souscrit" à Coaching indépendamment) aurait
/// dupliqué un état déjà représenté deux fois — voir <c>MainLayout.razor</c> pour le calcul de visibilité
/// réel des liens de navigation.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoachingModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' introuvable.");

        services.AddDbContextFactory<CoachingDbContext>(options =>
        {
            options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", CoachingDbContext.SchemaName));
        });

        services.AddScoped<ICoachingService, CoachingService>();

        // Implémentation réelle par-dessus le NoOp enregistré par AddSpectrometreCore — voir la remarque
        // équivalente dans Spectrometre.Modules.ProfilCandidat.
        services.AddScoped<ICoachingLinkOverviewService, CoachingLinkOverviewService>();

        return services;
    }
}
