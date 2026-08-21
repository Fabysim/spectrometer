using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
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
using Spectrometre.Modules.JeunesPrestataires;
using Spectrometre.Modules.JeunesPrestataires.Data;
using Spectrometre.Modules.Missions;
using Spectrometre.Modules.Missions.Data;
using Spectrometre.Modules.Recrutement;
using Spectrometre.Modules.Recrutement.Services;
using Spectrometre.Modules.ProfilCandidat;
using Spectrometre.Modules.ProfilCandidat.Services;
using Spectrometre.Modules.ProfilCoach;
using Spectrometre.Modules.ProfilCoach.Data;
using Spectrometre.Modules.ProfilEntreprise;
using Spectrometre.Modules.ProfilEntreprise.Services;
using Spectrometre.Modules.SuiviEvolutif;
using Spectrometre.Modules.SuiviEvolutif.Services;
using Spectrometre.Modules.SuiviEmployes;
using Spectrometre.Modules.Vivier;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Import de CV (PDF/docx jusqu'à 5 Mo) via InputFile en Blazor Server — le plafond SignalR par défaut (32 Ko)
// ferait échouer l'upload. Marge au-dessus de la limite métier.
builder.Services.Configure<Microsoft.AspNetCore.SignalR.HubOptions>(options =>
{
    options.MaximumReceiveMessageSize = 6 * 1024 * 1024;
});

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
builder.Services.AddJeunesPrestatairesModule(builder.Configuration);
builder.Services.AddMissionsModule(builder.Configuration);
builder.Services.AddProfilEntrepriseModule(builder.Configuration);
builder.Services.AddCompatibiliteModule(builder.Configuration);
builder.Services.AddRecrutementModule(builder.Configuration);
builder.Services.AddVivierModule();
builder.Services.AddEntretienModule(builder.Configuration);
builder.Services.AddSuiviEvolutifModule(builder.Configuration);
builder.Services.AddSuiviEmployesModule(builder.Configuration);
builder.Services.AddAnalyticsModule();
// Indépendant du domaine Matching Emploi (voir son manifeste) — l'ordre par rapport aux autres AddXxxModule
// n'a pas d'importance, aucune dépendance croisée.
builder.Services.AddGestionDuTempsModule(builder.Configuration);
builder.Services.AddHostedService<Spectrometre.Host.Workers.ActiviteNotificationWorker>();
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
builder.Services.AddScoped<ParticulierOnboardingService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
}).AddIdentityCookies();

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Data Protection : antiforgery Blazor / cookies. En Docker, persister sur volume (voir deploy/docker-compose.yml).
if (!builder.Environment.IsDevelopment())
{
    var keysPath = ResolveDataProtectionKeysPath(builder.Environment, builder.Configuration);
    Directory.CreateDirectory(keysPath);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
        .SetApplicationName("Spectrometre")
        .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
}

var useForwardedHeaders = builder.Configuration.GetValue("ReverseProxy:UseForwardedHeaders", false);
if (useForwardedHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

static string ResolveDataProtectionKeysPath(IHostEnvironment env, IConfiguration configuration)
{
    var configured = configuration["DataProtection:KeysPath"];
    if (string.IsNullOrWhiteSpace(configured))
        return Path.Combine(env.ContentRootPath, "keys");
    return Path.IsPathRooted(configured)
        ? configured
        : Path.Combine(env.ContentRootPath, configured);
}

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
    moduleRegistry.Register(Spectrometre.Modules.JeunesPrestataires.ServiceCollectionExtensions.Manifest);
    moduleRegistry.Register(Spectrometre.Modules.Missions.ServiceCollectionExtensions.Manifest);
    moduleRegistry.Register(Spectrometre.Modules.ProfilEntreprise.ServiceCollectionExtensions.Manifest);
    moduleRegistry.Register(Spectrometre.Modules.Compatibilite.ServiceCollectionExtensions.Manifest);
    moduleRegistry.Register(Spectrometre.Modules.Recrutement.ServiceCollectionExtensions.Manifest);
    moduleRegistry.Register(Spectrometre.Modules.Vivier.ServiceCollectionExtensions.Manifest);
    moduleRegistry.Register(Spectrometre.Modules.Entretien.ServiceCollectionExtensions.Manifest);
    moduleRegistry.Register(Spectrometre.Modules.SuiviEvolutif.ServiceCollectionExtensions.Manifest);
    moduleRegistry.Register(Spectrometre.Modules.SuiviEmployes.ServiceCollectionExtensions.Manifest);
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

    // Jeunes prestataires : schéma fixe non tenant-scopé, comme ProfilCandidat — migré globalement ici.
    using (var jeunesPrestatairesDb = startupScope.ServiceProvider
               .GetRequiredService<IDbContextFactory<JeunesPrestatairesDbContext>>()
               .CreateDbContext())
    {
        jeunesPrestatairesDb.Database.Migrate();
    }

    // Missions / Particulier : schéma fixe non tenant-scopé — migré globalement ici.
    using (var missionsDb = startupScope.ServiceProvider
               .GetRequiredService<IDbContextFactory<MissionsDbContext>>()
               .CreateDbContext())
    {
        missionsDb.Database.Migrate();
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

    // Catalogue partagé questions d'entrevue (schéma public) + seed idempotent (contenu MVP).
    using (var entretienCatalogDb = startupScope.ServiceProvider
               .GetRequiredService<IDbContextFactory<Spectrometre.Modules.Entretien.Data.EntretienCatalogDbContext>>()
               .CreateDbContext())
    {
        entretienCatalogDb.Database.Migrate();
        await Spectrometre.Modules.Entretien.Data.InterviewQuestionLibrarySeed.EnsureSeededAsync(entretienCatalogDb);
    }

    // Comble rétroactivement l'abonnement des entreprises créées avant l'exigence d'abonnement pour
    // l'accès effectif — sans ça, elles perdraient l'accès à tous leurs modules déjà activés (échec fermé, voir
    // ModuleRegistry.IsActiveAsync). Exécuté tôt, avant tout ce qui pourrait lire IsActiveAsync.
    await TenantSubscriptionBackfill.RunAsync(startupScope.ServiceProvider.GetRequiredService<CoreDbContext>());

    // Rétroactif : dédoublonne puis contraint à un seul CompanyProfile par schéma tenant déjà provisionné —
    // voir CompanyProfileUniquenessBackfill. Exécuté avant tout accès pouvant résoudre/créer un profil.
    await CompanyProfileUniquenessBackfill.RunAsync(startupScope.ServiceProvider);

    // Colonnes OffreTexte sur Postes (tenants déjà provisionnés) — SyncAll ne gère que les tables manquantes.
    await PosteOffreColumnsBackfill.RunAsync(startupScope.ServiceProvider);

    // NiveauDeclare + NiveauFinal nullable sur EvaluationsCriteresCandidature (tenants existants).
    await EvaluationCritereNiveauDeclareBackfill.RunAsync(startupScope.ServiceProvider);

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

if (useForwardedHeaders)
    app.UseForwardedHeaders();

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

// PDF de la charte — document de référence générique, tout coach authentifié (pas lié à un jeune).
// Même pattern que /candidat/cv/pdf (endpoint minimal : Blazor ne streame pas un binaire).
app.MapGet("/coach/charte/pdf", async (
    HttpContext httpContext,
    ICoachSubjectResolver coachSubjectResolver,
    Spectrometre.Modules.JeunesPrestataires.Services.IChartePdfService chartePdfService,
    IStringLocalizer<Spectrometre.Modules.JeunesPrestataires.Resources.JeunesPrestatairesResource> charteLocalizer) =>
{
    var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    if (await coachSubjectResolver.TryGetCoachProfileIdAsync(userId) is null)
        return Results.Forbid();

    var pdfBytes = chartePdfService.GeneratePdf(charteLocalizer);
    return Results.File(pdfBytes, "application/pdf", "charte-missions.pdf");
}).RequireAuthorization();

// PDF du consentement parental du jeune connecté — jamais un identifiant dans l'URL (même motif que
// /candidat/cv/pdf). Uniquement si le consentement est validé : il n'y a rien de définitif à télécharger avant.
app.MapGet("/candidat/consentement-parental/pdf", async (
    HttpContext httpContext,
    Spectrometre.Modules.JeunesPrestataires.Services.IJeuneProfileService jeuneProfileService,
    Spectrometre.Modules.JeunesPrestataires.Services.IConsentementParentalService consentementService,
    Spectrometre.Modules.JeunesPrestataires.Services.IConsentementParentalPdfService consentementPdfService,
    IStringLocalizer<Spectrometre.Modules.JeunesPrestataires.Resources.JeunesPrestatairesResource> consentementLocalizer) =>
{
    var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var jeune = await jeuneProfileService.TryGetByUserIdAsync(userId);
    if (jeune is null)
        return Results.NotFound();

    var vue = await consentementService.GetAsync(jeune.Id);
    if (!vue.EstValide)
        return Results.NotFound();

    var pdfBytes = consentementPdfService.GeneratePdf(jeune, vue, consentementLocalizer);
    return Results.File(pdfBytes, "application/pdf", "consentement-parental.pdf");
}).RequireAuthorization();

// Export PDF de l'analyse IA poste/candidature — même pattern que /candidat/cv/pdf (endpoint minimal
// car un composant Blazor ne streame pas un binaire). Accès : l'utilisateur doit être lié à
// l'entreprise propriétaire du poste (UserCompanyLink via GetCompaniesForUserAsync) ; la candidature
// est résolue dans le schéma tenant correspondant — jamais d'accès cross-entreprise.
app.MapGet("/entreprise/postes/{posteId:int}/candidats/{candidatureId:int}/analyse-ia/pdf", async (
    int posteId,
    int candidatureId,
    HttpContext httpContext,
    CoreDbContext coreDb,
    ITenantContext tenantContext,
    ICompanyProvisioningService companyProvisioning,
    IModuleRegistry moduleRegistry,
    IPosteService posteService,
    IRecrutementEntretienService recrutementEntretienService,
    ICandidateProfileService candidateProfileService,
    IAnalysePdfService analysePdfService) =>
{
    var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var companies = await companyProvisioning.GetCompaniesForUserAsync(userId, coreDb);
    var english = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "fr";

    foreach (var company in companies)
    {
        // Module Recrutement inactif = même traitement qu'une entreprise non autorisée (404, pas de fuite).
        if (!await moduleRegistry.IsActiveAsync(company.Id, "Recrutement", coreDb))
            continue;

        tenantContext.SetActiveCompany(company.Id, company.SchemaName);

        var candidature = await posteService.GetCandidatureAsync(candidatureId);
        if (candidature is null || candidature.PosteId != posteId)
            continue;

        var analyse = await recrutementEntretienService.GetAnalyseIaAsync(candidatureId);
        if (analyse is null)
            return Results.NotFound();

        var postes = await posteService.GetPostesAsync();
        var poste = postes.FirstOrDefault(p => p.Id == posteId);
        if (poste is null)
            return Results.NotFound();

        string? nomCandidat = null;
        try
        {
            var cv = await candidateProfileService.GetCvAsync(candidature.CandidateProfileId);
            var coord = cv.Coordonnees;
            if (coord is not null)
            {
                var parts = new[] { coord.Prenoms, coord.Nom }
                    .Where(s => !string.IsNullOrWhiteSpace(s));
                var joined = string.Join(' ', parts);
                if (!string.IsNullOrWhiteSpace(joined))
                    nomCandidat = joined;
            }
        }
        catch
        {
            // Nom optionnel — l'export reste valide avec l'id profil seul.
        }

        var pdfBytes = analysePdfService.GenerateAnalysePdf(new AnalysePdfModel(
            TitrePoste: poste.Titre,
            CandidateProfileId: candidature.CandidateProfileId,
            NomCandidat: nomCandidat,
            ScoreCompatibilite: candidature.ScoreCompatibilite,
            AnalyseTexte: analyse.AnalyseTexte,
            GenereeLe: analyse.GenereeLe,
            GenereeParIa: analyse.GenereeParIa,
            English: english));

        var safeTitre = string.Concat(poste.Titre.Where(ch => !Path.GetInvalidFileNameChars().Contains(ch)));
        if (string.IsNullOrWhiteSpace(safeTitre))
            safeTitre = "poste";
        var fileName = $"analyse-ia-{safeTitre}-{candidatureId}.pdf";

        return Results.File(pdfBytes, "application/pdf", fileName);
    }

    // Pas de fuite d'existence hors des entreprises de l'utilisateur.
    return Results.NotFound();
}).RequireAuthorization();

// Export .docx d'une offre d'emploi générée par IA — même pattern d'auth/tenant que analyse-ia/pdf.
// GET direct : le navigateur télécharge le fichier. Pas de cache GUID (contrairement au MVP).
app.MapGet("/entreprise/postes/{posteId:int}/offre/docx", async (
    int posteId,
    HttpContext httpContext,
    CoreDbContext coreDb,
    ITenantContext tenantContext,
    ICompanyProvisioningService companyProvisioning,
    IJobOfferDraftService jobOfferDraftService) =>
{
    var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var companies = await companyProvisioning.GetCompaniesForUserAsync(userId, coreDb);

    foreach (var company in companies)
    {
        tenantContext.SetActiveCompany(company.Id, company.SchemaName);

        var (content, fileName, erreur) = await jobOfferDraftService.GenererOffreDocxAsync(posteId);
        if (content is not null && !string.IsNullOrWhiteSpace(fileName))
        {
            return Results.File(
                content,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileName);
        }

        // Poste absent de ce schéma → essayer l'entreprise suivante (pas de fuite d'existence).
        if (string.Equals(erreur, JobOfferDraftService.ErreurPosteIntrouvable, StringComparison.Ordinal))
            continue;

        // Poste trouvé mais génération échouée (IA, etc.).
        if (!string.IsNullOrWhiteSpace(erreur))
            return Results.BadRequest(erreur);
    }

    return Results.NotFound();
}).RequireAuthorization();

// Liens de notification GDT : démarrer / terminer l'activité au clic, puis rediriger vers le Kanban.
// Même pattern d'auth que /candidat/cv/pdf — jamais de confirmation d'existence à un tiers (404).
app.MapGet("/gestion-du-temps/activite/{activiteId:int}/demarrer", async (
    int activiteId,
    HttpContext httpContext,
    Spectrometre.Modules.GestionDuTemps.Services.IActiviteNotificationActionService actions) =>
{
    var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    if (!await actions.DemarrerSiProprietaireAsync(userId, activiteId))
        return Results.NotFound();

    return Results.LocalRedirect("/gestion-du-temps/kanban?msg=activite-demarree");
}).RequireAuthorization();

app.MapGet("/gestion-du-temps/activite/{activiteId:int}/terminer", async (
    int activiteId,
    HttpContext httpContext,
    Spectrometre.Modules.GestionDuTemps.Services.IActiviteNotificationActionService actions) =>
{
    var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    if (!await actions.TerminerSiProprietaireAsync(userId, activiteId))
        return Results.NotFound();

    return Results.LocalRedirect("/gestion-du-temps/kanban?msg=activite-terminee");
}).RequireAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(Spectrometre.Host.Client._Imports).Assembly,
        typeof(Spectrometre.Modules.ProfilCandidat.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.JeunesPrestataires.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.Missions.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.ProfilEntreprise.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.Compatibilite.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.Recrutement.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.Vivier.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.Entretien.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.SuiviEvolutif.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.SuiviEmployes.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.Analytics.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.GestionDuTemps.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.ProfilCoach.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.Coaching.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.Admin.ServiceCollectionExtensions).Assembly);

app.Run();
