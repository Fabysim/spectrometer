using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;
using Spectrometre.Core.JeunesPrestataires;
using Spectrometre.Modules.JeunesPrestataires.Data;
using Spectrometre.Modules.JeunesPrestataires.Services;

namespace Spectrometre.Modules.JeunesPrestataires;

public static class ServiceCollectionExtensions
{
    public static readonly ModuleManifest Manifest = new(
        Code: "JeunesPrestataires",
        DisplayName: "Jeunes prestataires",
        DisplayNameEn: "Young service providers",
        Version: "1.0.0",
        RequiredModuleCodes: []);

    public static IServiceCollection AddJeunesPrestatairesModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' introuvable.");

        services.AddDbContextFactory<JeunesPrestatairesDbContext>(options =>
        {
            options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", JeunesPrestatairesDbContext.SchemaName));
        });

        services.AddScoped<IJeuneProfileService, JeuneProfileService>();
        services.AddScoped<IJeunePrestataireInvitationQuery, JeuneProfileService>();
        services.AddScoped<IJeunePrestatairePresence, JeunePrestatairePresence>();
        services.AddScoped<IConsentementParentalService, ConsentementParentalService>();
        services.AddScoped<IAutoObservationService, AutoObservationService>();
        services.AddScoped<IGrilleObservationService, GrilleObservationService>();
        services.AddScoped<IGuideEntrevueService, GuideEntrevueService>();
        services.AddScoped<IPlanActionAutoObservationService, PlanActionAutoObservationService>();
        services.AddScoped<ICharteService, CharteService>();
        services.AddScoped<IChartePdfService, ChartePdfService>();
        services.AddScoped<IConsentementParentalPdfService, ConsentementParentalPdfService>();
        return services;
    }
}
