using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectrometre.BackfillJeunesGdt;
using Spectrometre.Core;
using Spectrometre.Core.Data;
using Spectrometre.Core.Modules;
using Spectrometre.Modules.GestionDuTemps;
using Spectrometre.Modules.GestionDuTemps.Data;
using Spectrometre.Modules.JeunesPrestataires;
using Spectrometre.Modules.JeunesPrestataires.Data;
using Spectrometre.Modules.ProfilCandidat;
using Spectrometre.Modules.ProfilCandidat.Data;
using Spectrometre.Modules.ProfilCandidat.Services;
using GestionDuTempsModule = Spectrometre.Modules.GestionDuTemps.ServiceCollectionExtensions;

// Outil ponctuel — rattrapage Gestion du temps pour les jeunes prestataires créés avant l'activation
// automatique à l'acceptation d'invitation (InvitationAcceptancePage, JeunePrestataire).
//
// Usage :
//   dotnet run --project tools/Spectrometre.BackfillJeunesGdt
//   dotnet run --project tools/Spectrometre.BackfillJeunesGdt -- --ConnectionStrings:DefaultConnection=...
//
// Idempotent : les jeunes déjà actifs (HasAccessAsync) sont ignorés.
// À supprimer une fois exécuté en prod/staging si souhaité.
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Spectrometre.Host", "appsettings.json"), optional: true)
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Spectrometre.Host", "appsettings.Development.json"), optional: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddDataProtection();
services.AddSpectrometreCore(configuration);
services.AddProfilCandidatModule(configuration);
services.AddJeunesPrestatairesModule(configuration);
services.AddGestionDuTempsModule(configuration);
services.AddScoped<BackfillCandidateOnboarding>();

await using var provider = services.BuildServiceProvider();

using (var scope = provider.CreateScope())
{
    var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
    moduleRegistry.Register(Spectrometre.Modules.ProfilCandidat.ServiceCollectionExtensions.Manifest);
    moduleRegistry.Register(Spectrometre.Modules.GestionDuTemps.ServiceCollectionExtensions.Manifest);
    moduleRegistry.Register(Spectrometre.Modules.JeunesPrestataires.ServiceCollectionExtensions.Manifest);

    var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
    await coreDb.Database.MigrateAsync();

    await using var candidatDb = await scope.ServiceProvider
        .GetRequiredService<IDbContextFactory<ProfilCandidatDbContext>>()
        .CreateDbContextAsync();
    await candidatDb.Database.MigrateAsync();

    await using var jeunesDb = await scope.ServiceProvider
        .GetRequiredService<IDbContextFactory<JeunesPrestatairesDbContext>>()
        .CreateDbContextAsync();
    await jeunesDb.Database.MigrateAsync();

    await using var gdtDb = await scope.ServiceProvider
        .GetRequiredService<IDbContextFactory<GestionDuTempsDbContext>>()
        .CreateDbContextAsync();
    await gdtDb.Database.MigrateAsync();
}

using (var scope = provider.CreateScope())
{
    var jeunesDbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JeunesPrestatairesDbContext>>();
    var coreDbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<CoreDbContext>>();
    var candidateProfileService = scope.ServiceProvider.GetRequiredService<ICandidateProfileService>();
    var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
    var onboarding = scope.ServiceProvider.GetRequiredService<BackfillCandidateOnboarding>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("BackfillJeunesGdt");

    async Task<bool> HasGdtAccessAsync(string userId)
    {
        await using var coreDb = await coreDbFactory.CreateDbContextAsync();
        var candidateProfileId = await candidateProfileService.GetOrCreateProfileIdAsync(userId);
        return await moduleRegistry.IsActiveForCandidateAsync(
            candidateProfileId, GestionDuTempsModule.Manifest.Code, coreDb);
    }

    await using var jeunesDb = await jeunesDbFactory.CreateDbContextAsync();
    var jeunes = await jeunesDb.JeuneProfiles.AsNoTracking()
        .OrderBy(p => p.Id)
        .ToListAsync();

    var total = jeunes.Count;
    var dejaOk = 0;
    var misAJour = 0;
    var erreurs = 0;

    Console.WriteLine($"Jeunes prestataires trouvés : {total}");

    foreach (var jeune in jeunes)
    {
        if (await HasGdtAccessAsync(jeune.UserId))
        {
            dejaOk++;
            logger.LogInformation("Déjà actif — JeuneProfile #{Id} ({Prenoms} {Nom}, UserId={UserId})",
                jeune.Id, jeune.Prenoms, jeune.Nom, jeune.UserId);
            continue;
        }

        try
        {
            await using var coreDb = await coreDbFactory.CreateDbContextAsync();
            var candidateProfileId = await onboarding.CreateCandidateAsync(jeune.UserId, coreDb);
            await onboarding.ActivateGestionDuTempsAsync(candidateProfileId, coreDb);

            if (await HasGdtAccessAsync(jeune.UserId))
            {
                misAJour++;
                logger.LogInformation("Corrigé — JeuneProfile #{Id} ({Prenoms} {Nom})",
                    jeune.Id, jeune.Prenoms, jeune.Nom);
            }
            else
            {
                erreurs++;
                logger.LogWarning("Activation terminée mais HasAccessAsync=false — JeuneProfile #{Id}", jeune.Id);
            }
        }
        catch (Exception ex)
        {
            erreurs++;
            logger.LogError(ex, "Échec — JeuneProfile #{Id} ({Prenoms} {Nom})", jeune.Id, jeune.Prenoms, jeune.Nom);
        }
    }

    Console.WriteLine();
    Console.WriteLine("=== Résultat du rattrapage Gestion du temps (jeunes prestataires) ===");
    Console.WriteLine($"Total jeunes       : {total}");
    Console.WriteLine($"Déjà à jour        : {dejaOk}");
    Console.WriteLine($"Mis à jour         : {misAJour}");
    Console.WriteLine($"Erreurs            : {erreurs}");
}

return 0;
