using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Spectrometre.Core;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Recruitment;
using Spectrometre.Core.Suivi;
using Spectrometre.Core.Tenancy;
using Spectrometre.Host.Components;
using Spectrometre.Host.Onboarding;
using Spectrometre.Modules.Admin;
using Spectrometre.Modules.Analytics;
using Spectrometre.Modules.Coaching;
using Spectrometre.Modules.Coaching.Data;
using Spectrometre.Modules.Compatibilite;
using Spectrometre.Modules.Entretien;
using Spectrometre.Modules.GestionDuTemps;
using Spectrometre.Modules.GestionDuTemps.Data;
using Spectrometre.Modules.PostesRecrutement;
using Spectrometre.Modules.PostesRecrutement.Services;
using Spectrometre.Modules.ProfilCandidat;
using Spectrometre.Modules.ProfilCandidat.Services;
using Spectrometre.Modules.ProfilCoach;
using Spectrometre.Modules.ProfilCoach.Data;
using Spectrometre.Modules.ProfilEntreprise;
using Spectrometre.Modules.SuiviEvolutif;
using Spectrometre.Modules.SuiviEvolutif.Services;
using Spectrometre.Modules.Vivier;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Localisation du « chrome » de l'application (connexion, inscription, menu, tableau de bord) — voir
// Spectrometre.Host.Resources.SharedResource. Ne couvre PAS le contenu métier des modules (hors périmètre
// de ce cycle, voir leur propre remarque). Vit dans Host (jamais dans un module), donc aucune dépendance
// inter-module créée par la localisation.
// Pas de ResourcesPath explicite : SharedResource vit déjà dans le namespace/dossier Resources/, donc le
// nom de ressource intégré par défaut ("Spectrometre.Host.Resources.SharedResource") correspond directement
// au nom de fichier .resx compilé — fixer ResourcesPath="Resources" ici doublerait ce segment
// ("...Resources.Resources.SharedResource") et ferait échouer silencieusement toute résolution
// (IStringLocalizer retombe alors sur la clé brute, sans lever d'exception).
builder.Services.AddLocalization();
builder.Services.AddHttpContextAccessor();

// Noyau, puis chaque module — dans l'ordre de dépendance déclaré par son manifeste
// (Compatibilite dépend de ProfilCandidat + ProfilEntreprise).
builder.Services.AddSpectrometreCore(builder.Configuration);
builder.Services.AddProfilCandidatModule(builder.Configuration);
builder.Services.AddProfilEntrepriseModule(builder.Configuration);
builder.Services.AddCompatibiliteModule(builder.Configuration);
builder.Services.AddPostesRecrutementModule(builder.Configuration);
builder.Services.AddVivierModule();
builder.Services.AddEntretienModule(builder.Configuration);
builder.Services.AddSuiviEvolutifModule(builder.Configuration);
builder.Services.AddAnalyticsModule();
// Indépendant du domaine Matching Emploi (voir son manifeste) — l'ordre par rapport aux autres AddXxxModule
// n'a pas d'importance, aucune dépendance croisée.
builder.Services.AddGestionDuTempsModule(builder.Configuration);
// Idem : profil de base d'un 4e type de sujet (Coach), aucune dépendance croisée avec les modules ci-dessus.
builder.Services.AddProfilCoachModule(builder.Configuration);
// Coaching a de vraies dépendances de projet vers GestionDuTemps/ProfilCoach (voir son .csproj) — doit donc
// être enregistré après les deux. Pas de manifeste/activation propre, voir sa ServiceCollectionExtensions.
builder.Services.AddCoachingModule(builder.Configuration);
// Zone transverse /admin — jamais un sujet du registre d'activation généralisé, aucune activation à
// enregistrer (voir sa ServiceCollectionExtensions). Référence uniquement Core, jamais un autre module :
// les métadonnées candidat/coach/coaching lui parviennent via ICandidateDirectoryService/
// ICoachDirectoryService/ICoachingLinkOverviewService, déjà branchées par chaque AddXxxModule ci-dessus.
builder.Services.AddAdminModule(builder.Configuration);

// Inversion de dépendance : les pages GestionDuTemps consomment ICoachingAccessChecker (défini dans Core)
// sans connaître Coaching. C'est ICI, dans Host — le seul projet qui référence à la fois GestionDuTemps et
// Coaching — qu'on branche l'implémentation réelle par-dessus le NoOpCoachingAccessChecker enregistré par
// AddSpectrometreCore (la dernière inscription gagne à la résolution). Même recette que
// ICandidatureExistenceChecker/IProfileChangeRecorder ci-dessous.
builder.Services.AddScoped<Spectrometre.Core.Modules.ICoachingAccessChecker, Spectrometre.Modules.Coaching.Services.CoachingAccessChecker>();

// Inversion de dépendance : Compatibilite consomme ICandidatureExistenceChecker (défini dans Core) sans
// connaître son implémentation. C'est ICI, dans Host — le seul projet qui référence à la fois Compatibilite
// et PostesRecrutement — qu'on branche l'implémentation réelle, jamais depuis Compatibilite lui-même
// (qui ne doit pas obtenir de référence de projet vers PostesRecrutement : le manifeste déclare déjà la
// dépendance dans l'autre sens via Vivier, et un module ne dépend jamais de ce qui dépend de lui).
builder.Services.AddScoped<ICandidatureExistenceChecker, CandidatureExistenceChecker>();

// Même inversion de dépendance pour ProfilCandidat/ProfilEntreprise → SuiviEvolutif : l'implémentation
// réelle remplace ici le NoOpProfileChangeRecorder enregistré par AddSpectrometreCore (la dernière
// inscription d'un service gagne à la résolution).
builder.Services.AddScoped<IProfileChangeRecorder, ProfileChangeRecorder>();

builder.Services.AddScoped<CompanyOnboardingService>();
builder.Services.AddScoped<CandidateOnboardingService>();
builder.Services.AddScoped<CoachOnboardingService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
}).AddIdentityCookies();

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Français = culture par défaut (tout le contenu existant est en français) ; anglais = seule autre culture
// supportée pour l'instant. Cultures neutres ("fr"/"en", pas "fr-FR"/"en-US") : le chrome n'a pas besoin de
// variantes régionales, et ça garde SharedResource.resx/.en.resx alignés sans complexité supplémentaire.
string[] supportedCultures = ["fr", "en"];
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

var app = builder.Build();

// Registre des modules disponibles — Program.cs énumère explicitement ce qui est installé,
// conformément à l'architecture demandée (voir Spectrometre.Core.Modules.IModuleRegistry).
using (var startupScope = app.Services.CreateScope())
{
    var moduleRegistry = startupScope.ServiceProvider.GetRequiredService<IModuleRegistry>();
    moduleRegistry.Register(Spectrometre.Modules.ProfilCandidat.ServiceCollectionExtensions.Manifest);
    moduleRegistry.Register(Spectrometre.Modules.ProfilEntreprise.ServiceCollectionExtensions.Manifest);
    moduleRegistry.Register(Spectrometre.Modules.Compatibilite.ServiceCollectionExtensions.Manifest);
    moduleRegistry.Register(Spectrometre.Modules.PostesRecrutement.ServiceCollectionExtensions.Manifest);
    moduleRegistry.Register(Spectrometre.Modules.Vivier.ServiceCollectionExtensions.Manifest);
    moduleRegistry.Register(Spectrometre.Modules.Entretien.ServiceCollectionExtensions.Manifest);
    moduleRegistry.Register(Spectrometre.Modules.SuiviEvolutif.ServiceCollectionExtensions.Manifest);
    moduleRegistry.Register(Spectrometre.Modules.Analytics.ServiceCollectionExtensions.Manifest);
    // Enregistré pour les DEUX types de sujet (Company et Candidate) — le registre n'est plus couplé à la
    // seule entreprise (voir ModuleActivationSubjectType). Ne signifie PAS qu'il est activé par défaut pour
    // toute entreprise (voir le commentaire dans CompanyOnboardingService : vendu indépendamment).
    moduleRegistry.Register(Spectrometre.Modules.GestionDuTemps.ServiceCollectionExtensions.Manifest);
    // Enregistré pour Coach uniquement — voir ModuleActivationSubjectType.Coach.
    moduleRegistry.Register(Spectrometre.Modules.ProfilCoach.ServiceCollectionExtensions.Manifest);

    // Migrations appliquées globalement pour le noyau et Profil Candidat (schémas fixes, non tenant-scopés).
    // Profil Entreprise / Compatibilité sont tenant-scopés : leur schéma est appliqué par tenant lors
    // de la création d'une entreprise (voir CompanyOnboardingService + ITenantSchemaProvisioner).
    startupScope.ServiceProvider.GetRequiredService<CoreDbContext>().Database.Migrate();
    // ProfilCandidatDbContext est maintenant enregistré via AddDbContextFactory (voir ServiceCollectionExtensions
    // — instance fraîche par opération pour éviter tout usage concurrent d'un même DbContext) : il n'est plus
    // résolvable directement depuis le conteneur, on passe par la factory pour la migration au démarrage.
    using (var profilCandidatDb = startupScope.ServiceProvider
               .GetRequiredService<IDbContextFactory<Spectrometre.Modules.ProfilCandidat.Data.ProfilCandidatDbContext>>()
               .CreateDbContext())
    {
        profilCandidatDb.Database.Migrate();
    }

    // SuiviEvolutif candidat : schéma fixe non tenant-scopé, comme ProfilCandidat — migré globalement ici,
    // pas via TenantSchemaSynchronizer (réservé aux schémas PAR ENTREPRISE).
    using (var suiviEvolutifCandidatDb = startupScope.ServiceProvider
               .GetRequiredService<IDbContextFactory<Spectrometre.Modules.SuiviEvolutif.Data.SuiviEvolutifCandidatDbContext>>()
               .CreateDbContext())
    {
        suiviEvolutifCandidatDb.Database.Migrate();
    }

    // Gestion du temps : schéma fixe non tenant-scopé, comme ProfilCandidat — migré globalement ici. Pas de
    // schéma PAR ENTREPRISE (voir son ServiceCollectionExtensions) donc toujours absent de
    // TenantSchemaModuleCatalog/CompanyOnboardingService, mais désormais bien enregistré dans
    // IModuleRegistry (voir le commentaire sur son Manifest).
    using (var gestionDuTempsDb = startupScope.ServiceProvider
               .GetRequiredService<IDbContextFactory<GestionDuTempsDbContext>>()
               .CreateDbContext())
    {
        gestionDuTempsDb.Database.Migrate();
    }

    // Profil Coach : schéma fixe non tenant-scopé, comme ProfilCandidat — migré globalement ici.
    using (var profilCoachDb = startupScope.ServiceProvider
               .GetRequiredService<IDbContextFactory<ProfilCoachDbContext>>()
               .CreateDbContext())
    {
        profilCoachDb.Database.Migrate();
    }

    // Coaching : schéma fixe non tenant-scopé, comme Gestion du temps/Profil Coach — migré globalement ici.
    using (var coachingDb = startupScope.ServiceProvider
               .GetRequiredService<IDbContextFactory<CoachingDbContext>>()
               .CreateDbContext())
    {
        coachingDb.Database.Migrate();
    }

    // Amorce idempotente du rôle Admin — indispensable pour que le tout premier compte PlatformAdmin
    // (créé hors application, voir Spectrometre.AdminBootstrap) puisse être promu même si ce rôle n'a
    // jamais été créé auparavant. AdminService la refait aussi défensivement avant une promotion, au cas où
    // ce démarrage n'aurait jamais eu lieu (ex. base neuve manipulée directement par l'outil bootstrap).
    var roleManager = startupScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roleManager.RoleExistsAsync(PlatformRoles.Admin))
        await roleManager.CreateAsync(new IdentityRole(PlatformRoles.Admin));

    // Admin : schéma fixe non tenant-scopé (journal d'audit uniquement) — migré globalement ici, comme
    // ProfilCoach/Coaching.
    using (var adminDb = startupScope.ServiceProvider
               .GetRequiredService<IDbContextFactory<Spectrometre.Modules.Admin.Data.AdminDbContext>>()
               .CreateDbContext())
    {
        adminDb.Database.Migrate();
    }

    // Comble rétroactivement l'abonnement des entreprises créées avant le gating par plan introduit dans ce
    // cycle — sans ça, elles perdraient l'accès à tous leurs modules déjà activés (échec fermé, voir
    // ModuleRegistry.IsActiveAsync). Exécuté tôt, avant tout ce qui pourrait lire IsActiveAsync.
    await TenantSubscriptionBackfill.RunAsync(startupScope.ServiceProvider.GetRequiredService<CoreDbContext>());

    // Rétroactif : dédoublonne puis contraint à un seul CompanyProfile par schéma tenant déjà provisionné —
    // voir CompanyProfileUniquenessBackfill. Exécuté avant tout accès pouvant résoudre/créer un profil.
    await CompanyProfileUniquenessBackfill.RunAsync(startupScope.ServiceProvider);

    // Comble rétroactivement le schéma de tout module tenant-scopé marqué actif pour une entreprise
    // existante mais pas encore provisionné (ex. une entreprise créée avant l'ajout d'un module) — voir
    // TenantSchemaSynchronizer. Remplace les scripts one-off manuels utilisés jusqu'ici à chaque nouveau
    // module. Exécuté AVANT RecruitmentIndexBackfill, qui lit des schémas que cette synchronisation vient
    // de garantir présents.
    await TenantSchemaSynchronizer.SyncAllAsync(startupScope.ServiceProvider, TenantSchemaModuleCatalog.Modules);

    await RecruitmentIndexBackfill.RunAsync(startupScope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

// AVANT l'authentification/le routage des composants : la culture doit être résolue avant tout rendu Razor,
// y compris le rendu statique des pages de connexion/inscription. Note Blazor Server (voir Étape 1 du
// rapport) : pour un circuit interactif, cette résolution ne s'applique qu'à la requête HTTP initiale
// (préaffichage) — la culture reste ensuite fixée pour toute la durée du circuit, elle ne se re-résout
// jamais à chaque évènement SignalR. Changer de langue exige donc un rechargement complet de page (voir
// /culture/set ci-dessous et le lien de sélection dans MainLayout, un <a> ordinaire avec navigation
// forcée — jamais un gestionnaire d'évènement Blazor, qui ne rouvrirait pas de nouveau circuit).
app.UseRequestLocalization(localizationOptions);

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Change la langue : écrit le cookie de culture standard ASP.NET Core puis redirige vers la page d'origine
// (jamais un gestionnaire Blazor — voir la remarque sur UseRequestLocalization ci-dessus). Results.LocalRedirect
// refuse toute URL non locale (protection open-redirect intégrée), donc sûr même si redirectUri provient
// d'un lien public non authentifié (connexion/inscription).
app.MapGet("/culture/set", (string culture, string redirectUri, HttpContext httpContext) =>
{
    if (Array.IndexOf(supportedCultures, culture) >= 0)
    {
        httpContext.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });
    }

    return Results.LocalRedirect(redirectUri);
});

// Export PDF du propre CV du candidat connecté — jamais un paramètre candidateProfileId dans l'URL : la
// route ne prend aucun identifiant, résolu exclusivement depuis l'utilisateur authentifié (voir
// ICandidateSubjectResolver), donc structurellement impossible de demander le CV de quelqu'un d'autre par
// cet endpoint. Endpoint minimal plutôt qu'un lien direct depuis une page Blazor : un composant ne peut pas
// streamer un fichier binaire vers le navigateur (voir la remarque sur ICvPdfService).
app.MapGet("/candidat/cv/pdf", async (
    HttpContext httpContext,
    Spectrometre.Core.Modules.ICandidateSubjectResolver candidateSubjectResolver,
    ICandidateProfileService candidateProfileService,
    ICvPdfService cvPdfService) =>
{
    var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var candidateProfileId = await candidateSubjectResolver.GetOrCreateCandidateProfileIdAsync(userId);
    var cv = await candidateProfileService.GetCvAsync(candidateProfileId);
    var pdfBytes = cvPdfService.GenerateCvPdf(cv);

    return Results.File(pdfBytes, "application/pdf", "cv.pdf");
}).RequireAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(Spectrometre.Host.Client._Imports).Assembly,
        typeof(Spectrometre.Modules.ProfilCandidat.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.ProfilEntreprise.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.Compatibilite.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.PostesRecrutement.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.Vivier.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.Entretien.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.SuiviEvolutif.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.Analytics.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.GestionDuTemps.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.ProfilCoach.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.Coaching.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.Admin.ServiceCollectionExtensions).Assembly);

app.Run();
