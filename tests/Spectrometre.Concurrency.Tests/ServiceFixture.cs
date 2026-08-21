using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Spectrometre.Core.Ai;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Email;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Invitations;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Notifications;
using Spectrometre.Core.Recruitment;
using Spectrometre.Core.Suivi;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Admin;
using Spectrometre.Modules.Analytics;
using Spectrometre.Modules.Coaching;
using Spectrometre.Modules.Coaching.Data;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.Compatibilite;
using Spectrometre.Modules.Compatibilite.Data;
using Spectrometre.Modules.Entretien;
using Spectrometre.Modules.Entretien.Data;
using Spectrometre.Modules.GestionDuTemps;
using Spectrometre.Modules.GestionDuTemps.Data;
using Spectrometre.Modules.JeunesPrestataires;
using Spectrometre.Modules.JeunesPrestataires.Data;
using Spectrometre.Modules.JeunesPrestataires.Services;
using Spectrometre.Modules.Missions;
using Spectrometre.Modules.Missions.Data;
using Spectrometre.Modules.GestionDuTemps.Services;
using Spectrometre.Modules.ProfilCoach;
using Spectrometre.Modules.ProfilCoach.Data;
using Spectrometre.Modules.Recrutement;
using Spectrometre.Modules.Recrutement.Data;
using Spectrometre.Modules.Recrutement.Services;
using Spectrometre.Modules.ProfilCandidat;
using Spectrometre.Modules.ProfilCandidat.Data;
using Spectrometre.Modules.ProfilEntreprise;
using Spectrometre.Modules.ProfilEntreprise.Data;
using Spectrometre.Modules.ProfilEntreprise.Services;
using Spectrometre.Modules.SuiviEvolutif;
using Spectrometre.Modules.SuiviEvolutif.Data;
using Spectrometre.Modules.SuiviEvolutif.Services;
using Spectrometre.Modules.SuiviEmployes;
using Spectrometre.Modules.SuiviEmployes.Data;
using Spectrometre.Modules.Vivier;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Construit un conteneur DI minimal, exactement avec les mêmes extensions <c>AddXxxModule</c> que
/// <c>Spectrometre.Host.Program</c>, pour tester les services publics tels qu'ils sont réellement
/// utilisés — pas des doublures. Nécessite une instance Postgres accessible (voir <see cref="ConnectionString"/>) :
/// mêmes migrations que l'application réelle, appliquées une fois ici. Un <see cref="ITenantContext"/>
/// neutre pointant vers le schéma "public" est utilisé pour le côté entreprise — ce schéma "gabarit"
/// n'héberge jamais de vraie entreprise (voir ITenantSchemaNameGenerator), c'est donc un bac à sable sûr.
/// </summary>
/// <remarks>
/// N'appelle PAS <c>AddSpectrometreCore</c> dans son ensemble (qui suppose un hôte ASP.NET Core complet
/// pour certaines options) : seul le sous-ensemble réellement utilisé par les tests est reproduit
/// (CoreDbContext, registre de modules, provisioning). Identity (UserManager/RoleManager/SignInManager) a
/// été ajouté ci-dessous — a minima, sans <c>RequireConfirmedAccount</c> — spécifiquement pour le cycle
/// Admin : <c>AdminService.PromouvoirAsync</c>/<c>RetrograderAsync</c> manipulent réellement des comptes et
/// des rôles Identity, aucun double ne peut se substituer à ça sans re-tester une abstraction fictive.
/// </remarks>
public sealed class ServiceFixture : IAsyncLifetime
{
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("SPECTROMETRE_TEST_CONNECTION")
        ?? "Host=localhost;Port=5432;Database=spectrometre_v2;Username=postgres;Password=Pil@tes2025";

    public ServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// Entreprises créées pendant la suite, nettoyées une seule fois en fin d'exécution (voir DisposeAsync)
    /// plutôt que test par test : chaque entreprise provisionne un VRAI schéma Postgres dédié dans la base de
    /// développement partagée (voir ConnectionString), jamais nettoyé sinon — RecruitmentIndexBackfill au
    /// démarrage du Host le redécouvrirait alors indéfiniment à chaque exécution de la suite et échouerait
    /// dessus si son schéma PostesRecrutement n'existe pas (comportement caught/attendu, mais bruit qui
    /// grossit sans fin). ConcurrentBag plutôt que List : xUnit peut exécuter les méthodes d'une même classe
    /// de test en parallèle par défaut.
    /// </summary>
    private readonly System.Collections.Concurrent.ConcurrentBag<Company> _companiesToCleanup = [];

    /// <summary>
    /// À appeler pour toute entreprise créée SANS passer par <see cref="CreateCompanyAsync"/> (qui s'en charge
    /// déjà lui-même) — ex. <c>ICompanyProvisioningService.CreateCompanyAsync</c> appelé directement dans
    /// ModuleActivationTests pour simuler une entreprise antérieure à la création d'abonnement.
    /// </summary>
    public void TrackCompanyForCleanup(Company company) => _companiesToCleanup.Add(company);

    /// <summary>
    /// Comptes Identity créés pendant la suite (ex. promotion/rétrogradation PlatformAdmin dans AdminTests) —
    /// même raisonnement que <see cref="_companiesToCleanup"/> : sans ça, chaque exécution de la suite
    /// laisserait des lignes <c>AspNetUsers</c>/<c>AspNetUserRoles</c> orphelines dans la base de
    /// développement partagée, faussant à terme les compteurs globaux de la zone Admin.
    /// </summary>
    private readonly System.Collections.Concurrent.ConcurrentBag<string> _usersToCleanup = [];

    public void TrackUserForCleanup(string userId) => _usersToCleanup.Add(userId);

    /// <summary>
    /// Les tests de missions supposent un jeune qui a déjà accepté la charte.
    /// Les jeunes de développement existants ne sont pas rattrapés ici.
    /// </summary>
    public async Task GarantirCharteAccepteeAsync(string jeuneUserId)
    {
        var charte = Services.GetRequiredService<ICharteService>();
        if (await charte.EstAccepteeAsync(jeuneUserId))
            return;
        Assert.True(await charte.AccepterAsync(jeuneUserId, "Confirmation test"));
    }

    public async Task InitializeAsync()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
            })
            .Build();

        var services = new ServiceCollection();

        Action<DbContextOptionsBuilder> configureCoreDbContext = options =>
        {
            options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
            options.UseNpgsql(ConnectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "core"));
        };
        // Voir Spectrometre.Core.ServiceCollectionExtensions.AddSpectrometreCore : CoreDbContext est
        // enregistré Scoped via un délégué sur la factory plutôt que par un second appel à AddDbContext, qui
        // enregistrerait une configuration DbContextOptions concurrente (dépendance captive + résolution
        // cassée pour les outils de conception EF Core).
        services.AddDbContextFactory<CoreDbContext>(configureCoreDbContext);
        services.AddScoped<CoreDbContext>(sp => sp.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContext());

        // Voir la remarque sur ServiceFixture : sous-ensemble d'AddSpectrometreCore réellement nécessaire
        // (UserManager/RoleManager pour AdminService), sans RequireConfirmedAccount (les tests ne se
        // connectent jamais par mot de passe, seulement par manipulation directe de comptes/rôles). Pas de
        // AddDefaultTokenProviders() : ça exigerait aussi AddDataProtection() (fourni gratuitement par un
        // hôte ASP.NET Core complet, absent ici) — inutile puisqu'aucun test n'appelle de méthode générant
        // un jeton (confirmation email, réinitialisation de mot de passe).
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<CoreDbContext>()
            .AddSignInManager();

        services.AddSingleton<ITenantSchemaNameGenerator, TenantSchemaNameGenerator>();
        services.AddScoped<ICompanyProvisioningService, CompanyProvisioningService>();
        services.AddScoped<ITenantSchemaProvisioner, TenantSchemaProvisioner>();
        services.AddSingleton<IModuleRegistry, ModuleRegistry>();
        services.AddScoped<IRecruitmentIndexService, RecruitmentIndexService>();
        services.AddScoped<IInvitationService, InvitationService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPreferenceNotificationService, PreferenceNotificationService>();
        services.AddScoped<IFacturationCalculatorService, FacturationCalculatorService>();

        services.AddScoped<ITenantContext, TenantContext>();
        services.AddProfilCandidatModule(config);
        services.AddJeunesPrestatairesModule(config);
        services.AddMissionsModule(config);
        services.AddProfilEntrepriseModule(config);
        services.AddCompatibiliteModule(config);
        services.AddRecrutementModule(config);
        services.AddEntretienModule(config);
        services.AddSuiviEvolutifModule(config);
        services.AddSuiviEmployesModule(config);
        services.AddAnalyticsModule();
        services.AddGestionDuTempsModule(config);
        services.AddProfilCoachModule(config);
        services.AddCoachingModule(config);
        services.AddAdminModule(config);
        services.AddVivierModule();

        // Même câblage que Spectrometre.Host.Program pour ICoachingAccessChecker : implémentation réelle
        // par-dessus le no-op (absent ici, on l'enregistre directement, comme pour IProfileChangeRecorder).
        services.AddScoped<ICoachingAccessChecker, CoachingAccessChecker>();

        // Jamais d'appel réseau réel à Replicate en test — substitue l'implémentation réelle enregistrée
        // par AddGestionDuTempsModule / AddSpectrometreCore par des doubles configurables.
        services.AddScoped<IReplicateService, FakeReplicateService>();
        services.AddScoped<IResendEmailService, FakeResendEmailService>();
        services.Replace(ServiceDescriptor.Scoped<IAiSynthesisService, FakeAiSynthesisService>());
        services.Replace(ServiceDescriptor.Scoped<Spectrometre.Modules.Recrutement.Services.IAnalysePosteIaService, FakeAnalysePosteIaService>());
        services.Replace(ServiceDescriptor.Scoped<Spectrometre.Modules.ProfilEntreprise.Services.IPosteCritereIaService, FakePosteCritereIaService>());

        // Même câblage que Spectrometre.Host.Program : l'implémentation réelle est fournie par
        // ProfilEntreprise (ex-PostesRecrutement) mais enregistrée ici (pas depuis Compatibilite).
        services.AddScoped<ICandidatureExistenceChecker, CandidatureExistenceChecker>();

        // Idem pour ProfilCandidat/ProfilEntreprise → SuiviEvolutif : implémentation réelle par-dessus le
        // no-op (absent ici puisqu'on n'appelle pas AddSpectrometreCore — on l'enregistre directement).
        services.AddScoped<IProfileChangeRecorder, ProfileChangeRecorder>();

        services.AddLocalization();

        Services = services.BuildServiceProvider();

        using (var scope = Services.CreateScope())
        {
            var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
            moduleRegistry.Register(Spectrometre.Modules.ProfilCandidat.ServiceCollectionExtensions.Manifest);
            moduleRegistry.Register(Spectrometre.Modules.JeunesPrestataires.ServiceCollectionExtensions.Manifest);
            moduleRegistry.Register(Spectrometre.Modules.Missions.ServiceCollectionExtensions.Manifest);
            moduleRegistry.Register(Spectrometre.Modules.ProfilEntreprise.ServiceCollectionExtensions.Manifest);
            moduleRegistry.Register(Spectrometre.Modules.Compatibilite.ServiceCollectionExtensions.Manifest);
            moduleRegistry.Register(Spectrometre.Modules.Recrutement.ServiceCollectionExtensions.Manifest);
            moduleRegistry.Register(Spectrometre.Modules.Entretien.ServiceCollectionExtensions.Manifest);
            moduleRegistry.Register(Spectrometre.Modules.SuiviEvolutif.ServiceCollectionExtensions.Manifest);
            moduleRegistry.Register(Spectrometre.Modules.SuiviEmployes.ServiceCollectionExtensions.Manifest);
            moduleRegistry.Register(Spectrometre.Modules.Analytics.ServiceCollectionExtensions.Manifest);
            moduleRegistry.Register(Spectrometre.Modules.GestionDuTemps.ServiceCollectionExtensions.Manifest);
            moduleRegistry.Register(Spectrometre.Modules.ProfilCoach.ServiceCollectionExtensions.Manifest);
            moduleRegistry.Register(Spectrometre.Modules.Vivier.ServiceCollectionExtensions.Manifest);
            // Coaching n'a pas de manifeste propre (voir sa ServiceCollectionExtensions).

            var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            await coreDb.Database.MigrateAsync();
        }

        // Les migrations de ProfilCandidat (schéma fixe) et le schéma "gabarit" public de
        // ProfilEntreprise/Compatibilite/PostesRecrutement (tenant-scopés) doivent déjà être en place —
        // idempotent.
        await using var candidatDb = await Services.GetRequiredService<IDbContextFactory<ProfilCandidatDbContext>>().CreateDbContextAsync();
        await candidatDb.Database.MigrateAsync();

        await using var jeunesPrestatairesDb = await Services.GetRequiredService<IDbContextFactory<JeunesPrestatairesDbContext>>().CreateDbContextAsync();
        await jeunesPrestatairesDb.Database.MigrateAsync();

        await using var missionsDb = await Services.GetRequiredService<IDbContextFactory<MissionsDbContext>>().CreateDbContextAsync();
        await missionsDb.Database.MigrateAsync();

        await using var entrepriseDb = await Services.GetRequiredService<IDbContextFactory<ProfilEntrepriseDbContext>>().CreateDbContextAsync();
        await entrepriseDb.Database.MigrateAsync();

        await using var compatibiliteDb = await Services.GetRequiredService<IDbContextFactory<CompatibiliteDbContext>>().CreateDbContextAsync();
        await compatibiliteDb.Database.MigrateAsync();

        await using var postesDb = await Services.GetRequiredService<IDbContextFactory<RecrutementDbContext>>().CreateDbContextAsync();
        await postesDb.Database.MigrateAsync();

        await using var entretienDb = await Services.GetRequiredService<IDbContextFactory<EntretienDbContext>>().CreateDbContextAsync();
        await entretienDb.Database.MigrateAsync();

        await using var entretienCatalogDb = await Services.GetRequiredService<IDbContextFactory<EntretienCatalogDbContext>>().CreateDbContextAsync();
        await entretienCatalogDb.Database.MigrateAsync();
        await InterviewQuestionLibrarySeed.EnsureSeededAsync(entretienCatalogDb);

        await using var suiviCandidatDb = await Services.GetRequiredService<IDbContextFactory<SuiviEvolutifCandidatDbContext>>().CreateDbContextAsync();
        await suiviCandidatDb.Database.MigrateAsync();

        await using var suiviEntrepriseDb = await Services.GetRequiredService<IDbContextFactory<SuiviEvolutifEntrepriseDbContext>>().CreateDbContextAsync();
        await suiviEntrepriseDb.Database.MigrateAsync();

        await using var suiviEmployesDb = await Services.GetRequiredService<IDbContextFactory<SuiviEmployesDbContext>>().CreateDbContextAsync();
        await suiviEmployesDb.Database.MigrateAsync();

        await using var gestionDuTempsDb = await Services.GetRequiredService<IDbContextFactory<GestionDuTempsDbContext>>().CreateDbContextAsync();
        await gestionDuTempsDb.Database.MigrateAsync();

        await using var profilCoachDb = await Services.GetRequiredService<IDbContextFactory<ProfilCoachDbContext>>().CreateDbContextAsync();
        await profilCoachDb.Database.MigrateAsync();

        await using var coachingDb = await Services.GetRequiredService<IDbContextFactory<CoachingDbContext>>().CreateDbContextAsync();
        await coachingDb.Database.MigrateAsync();

        await using var adminDb = await Services.GetRequiredService<IDbContextFactory<Spectrometre.Modules.Admin.Data.AdminDbContext>>().CreateDbContextAsync();
        await adminDb.Database.MigrateAsync();
    }

    /// <summary>
    /// Catalogue local des modules tenant-scopés, même principe que
    /// <c>Spectrometre.Host.Onboarding.TenantSchemaModuleCatalog</c> (le projet de test ne référence pas
    /// Host) — utilisé à la fois par <see cref="CreateCompanyAsync"/> et par les tests de synchronisation.
    /// </summary>
    public static readonly IReadOnlyList<TenantSchemaModule> TenantModules =
    [
        new(Spectrometre.Modules.ProfilEntreprise.ServiceCollectionExtensions.Manifest.Code,
            async (sp, ct) => await sp.GetRequiredService<IDbContextFactory<ProfilEntrepriseDbContext>>().CreateDbContextAsync(ct)),
        new(Spectrometre.Modules.Compatibilite.ServiceCollectionExtensions.Manifest.Code,
            async (sp, ct) => await sp.GetRequiredService<IDbContextFactory<CompatibiliteDbContext>>().CreateDbContextAsync(ct)),
        new(Spectrometre.Modules.Recrutement.ServiceCollectionExtensions.Manifest.Code,
            async (sp, ct) => await sp.GetRequiredService<IDbContextFactory<RecrutementDbContext>>().CreateDbContextAsync(ct)),
        new(Spectrometre.Modules.Entretien.ServiceCollectionExtensions.Manifest.Code,
            async (sp, ct) => await sp.GetRequiredService<IDbContextFactory<EntretienDbContext>>().CreateDbContextAsync(ct)),
        new(Spectrometre.Modules.SuiviEvolutif.ServiceCollectionExtensions.Manifest.Code,
            async (sp, ct) => await sp.GetRequiredService<IDbContextFactory<SuiviEvolutifEntrepriseDbContext>>().CreateDbContextAsync(ct)),
        new(Spectrometre.Modules.SuiviEmployes.ServiceCollectionExtensions.Manifest.Code,
            async (sp, ct) => await sp.GetRequiredService<IDbContextFactory<SuiviEmployesDbContext>>().CreateDbContextAsync(ct)),
    ];

    /// <summary>
    /// Crée une entreprise de test complète : schéma provisionné, propriétaire lié (<c>UserCompanyLink</c>),
    /// et tous les modules tenant-scopés activés — le strict nécessaire pour que
    /// <c>CandidatureExistenceChecker</c> puisse réellement trouver une candidature dans ce tenant.
    /// </summary>
    /// <param name="skipSchemaForModuleCodes">
    /// Codes de module à activer SANS provisionner leur schéma — simule une entreprise "en retard" (module
    /// actif mais schéma jamais appliqué), pour tester <c>TenantSchemaSynchronizer</c>.
    /// </param>
    public async Task<Company> CreateCompanyAsync(string name, string ownerUserId, IReadOnlyCollection<string>? skipSchemaForModuleCodes = null)
    {
        skipSchemaForModuleCodes ??= [];

        using var scope = Services.CreateScope();
        var provisioning = scope.ServiceProvider.GetRequiredService<ICompanyProvisioningService>();
        var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
        var provisioner = scope.ServiceProvider.GetRequiredService<ITenantSchemaProvisioner>();
        var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var company = await provisioning.CreateCompanyAsync(name, ownerUserId, coreDb);

        // Même câblage que Spectrometre.Host.Onboarding.CompanyOnboardingService : sans abonnement, aucun
        // module ne serait effectivement actif pour cette entreprise (échec fermé, voir
        // ModuleRegistry.IsActiveAsync) — Standard couvre les 8 modules Matching Emploi, pas GestionDuTemps.
        coreDb.TenantSubscriptions.Add(new TenantSubscription
        {
            CompanyId = company.Id,
            PlanCode = PlanCodes.Standard,
            Status = SubscriptionStatus.Active,
        });
        await coreDb.SaveChangesAsync();

        foreach (var module in TenantModules)
        {
            if (skipSchemaForModuleCodes.Contains(module.ModuleCode))
                continue;

            await using var db = await module.CreateDbContextAsync(Services, default);
            await provisioner.ApplyModuleSchemaAsync(db, "public", company.SchemaName);
        }

        // Ordre imposé par les dépendances déclarées aux manifestes (Compatibilite requiert ProfilEntreprise ;
        // PostesRecrutement requiert ProfilEntreprise ; Entretien requiert Compatibilite). ProfilCandidat
        // n'est PLUS activé ici pour le sujet Company (cycle correctif « modules personnels ») : Compatibilite
        // ne le liste plus comme dépendance (voir son manifeste) — l'activer artificiellement pour l'entreprise
        // n'a jamais servi à rien à l'exécution et faisait apparaître à tort ce module personnel dans son menu.
        await moduleRegistry.ActivateForCompanyAsync(company.Id, Spectrometre.Modules.ProfilEntreprise.ServiceCollectionExtensions.Manifest.Code, coreDb);
        await moduleRegistry.ActivateForCompanyAsync(company.Id, Spectrometre.Modules.Compatibilite.ServiceCollectionExtensions.Manifest.Code, coreDb);
        await moduleRegistry.ActivateForCompanyAsync(company.Id, Spectrometre.Modules.Recrutement.ServiceCollectionExtensions.Manifest.Code, coreDb);
        await moduleRegistry.ActivateForCompanyAsync(company.Id, Spectrometre.Modules.Entretien.ServiceCollectionExtensions.Manifest.Code, coreDb);
        await moduleRegistry.ActivateForCompanyAsync(company.Id, Spectrometre.Modules.SuiviEvolutif.ServiceCollectionExtensions.Manifest.Code, coreDb);
        // Analytics n'a pas de schéma propre (voir TenantModules ci-dessus, volontairement absent) — rien à
        // provisionner, seulement l'activation.
        await moduleRegistry.ActivateForCompanyAsync(company.Id, Spectrometre.Modules.Analytics.ServiceCollectionExtensions.Manifest.Code, coreDb);

        TrackCompanyForCleanup(company);
        return company;
    }

    public async Task DisposeAsync()
    {
        await using (var coreDb = await Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync())
        {
            foreach (var company in _companiesToCleanup)
                await CleanupCompanyAsync(coreDb, company);
        }

        using (var scope = Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            foreach (var userId in _usersToCleanup)
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user is not null)
                    await userManager.DeleteAsync(user); // Cascade sur AspNetUserRoles (modèle Identity par défaut).
            }
        }

        await Services.DisposeAsync();
    }

    private static async Task CleanupCompanyAsync(CoreDbContext coreDb, Company company)
    {
        var activations = await coreDb.ModuleActivations
            .Where(a => a.SubjectType == ModuleActivationSubjectType.Company && a.SubjectId == company.Id)
            .ToListAsync();
        coreDb.ModuleActivations.RemoveRange(activations);

        var subscriptions = await coreDb.TenantSubscriptions.Where(s => s.CompanyId == company.Id).ToListAsync();
        coreDb.TenantSubscriptions.RemoveRange(subscriptions);

        await coreDb.SaveChangesAsync();

        // Cascade sur UserCompanyLinks (voir OnModelCreating de CoreDbContext).
        coreDb.Companies.Remove(company);
        await coreDb.SaveChangesAsync();

        // Concaténation plutôt qu'une chaîne interpolée (voir EF1002) : schéma déjà validé par
        // ICompanyProvisioningService.QuoteValidatedSchema à sa création, revalidé ici par défense en
        // profondeur puisque DROP SCHEMA ne peut pas être paramétré comme une valeur (identifiant, pas
        // littéral).
        if (!System.Text.RegularExpressions.Regex.IsMatch(company.SchemaName, "^[a-z][a-z0-9_]{0,62}$"))
            throw new InvalidOperationException("Schéma invalide.");
        await coreDb.Database.ExecuteSqlRawAsync("DROP SCHEMA IF EXISTS \"" + company.SchemaName + "\" CASCADE;");
    }
}

[CollectionDefinition("Base de données partagée")]
public sealed class DatabaseCollection : ICollectionFixture<ServiceFixture>;
