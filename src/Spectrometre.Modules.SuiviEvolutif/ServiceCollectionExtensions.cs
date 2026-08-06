using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.SuiviEvolutif.Data;
using Spectrometre.Modules.SuiviEvolutif.Services;

namespace Spectrometre.Modules.SuiviEvolutif;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// <c>RequiredModuleCodes</c> ne liste que ProfilEntreprise — ProfilCandidat en a été retiré, pour la
    /// même raison que Compatibilite (voir son manifeste) : le côté candidat de Suivi évolutif est TOUJOURS
    /// tracé (<see cref="Services.ProfileChangeRecorder"/>, schéma fixe, jamais gaté par une activation de
    /// module — voir sa remarque), et le côté entreprise ne consulte que sa PROPRE activation
    /// (<c>IsActiveAsync(companyId, "SuiviEvolutif", ...)</c>), jamais celle de ProfilCandidat pour ce
    /// sujet. Cette dépendance ne servait qu'à forcer une ligne <c>ModuleActivation</c> artificielle sur le
    /// sujet Company, faisant apparaître à tort « Profil Candidat » (module personnel) dans le menu/tableau
    /// de bord d'une entreprise ayant activé Suivi évolutif.
    /// </summary>
    public static readonly ModuleManifest Manifest = new(
        Code: "SuiviEvolutif",
        DisplayName: "Suivi évolutif",
        DisplayNameEn: "Change History",
        Version: "1.0.0",
        RequiredModuleCodes: ["ProfilEntreprise"]);

    public static IServiceCollection AddSuiviEvolutifModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' introuvable.");

        // Schéma fixe (voir SuiviEvolutifCandidatDbContext) : IDbContextFactory quand même, pour éviter
        // tout usage concurrent d'un même DbContext partagé par circuit Blazor Server (même raison que
        // ProfilCandidatDbContext).
        services.AddDbContextFactory<SuiviEvolutifCandidatDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", SuiviEvolutifCandidatDbContext.SchemaName));
        });

        // Tenant-scopé (voir SuiviEvolutifEntrepriseDbContext).
        services.AddDbContextFactory<SuiviEvolutifEntrepriseDbContext>(options =>
        {
            options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_SuiviEvolutifEntreprise", "public"));
        });

        services.AddScoped<ISuiviEvolutifService, SuiviEvolutifService>();

        // L'enregistrement de IProfileChangeRecorder (implémentation réelle, par-dessus le no-op de Core)
        // se fait depuis Program.cs, pas ici — voir le commentaire sur ProfileChangeRecorder.

        return services;
    }
}
