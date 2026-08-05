using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Spectrometre.Core;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Modules;
using Spectrometre.Host.Components;
using Spectrometre.Host.Onboarding;
using Spectrometre.Modules.Compatibilite;
using Spectrometre.Modules.ProfilCandidat;
using Spectrometre.Modules.ProfilEntreprise;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Noyau, puis chaque module — dans l'ordre de dépendance déclaré par son manifeste
// (Compatibilite dépend de ProfilCandidat + ProfilEntreprise).
builder.Services.AddSpectrometreCore(builder.Configuration);
builder.Services.AddProfilCandidatModule(builder.Configuration);
builder.Services.AddProfilEntrepriseModule(builder.Configuration);
builder.Services.AddCompatibiliteModule(builder.Configuration);

builder.Services.AddScoped<CompanyOnboardingService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
}).AddIdentityCookies();

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Registre des modules disponibles — Program.cs énumère explicitement ce qui est installé,
// conformément à l'architecture demandée (voir Spectrometre.Core.Modules.IModuleRegistry).
using (var startupScope = app.Services.CreateScope())
{
    var moduleRegistry = startupScope.ServiceProvider.GetRequiredService<IModuleRegistry>();
    moduleRegistry.Register(Spectrometre.Modules.ProfilCandidat.ServiceCollectionExtensions.Manifest);
    moduleRegistry.Register(Spectrometre.Modules.ProfilEntreprise.ServiceCollectionExtensions.Manifest);
    moduleRegistry.Register(Spectrometre.Modules.Compatibilite.ServiceCollectionExtensions.Manifest);

    // Migrations appliquées globalement pour le noyau et Profil Candidat (schémas fixes, non tenant-scopés).
    // Profil Entreprise / Compatibilité sont tenant-scopés : leur schéma est appliqué par tenant lors
    // de la création d'une entreprise (voir CompanyOnboardingService + ITenantSchemaProvisioner).
    startupScope.ServiceProvider.GetRequiredService<CoreDbContext>().Database.Migrate();
    startupScope.ServiceProvider.GetRequiredService<Spectrometre.Modules.ProfilCandidat.Data.ProfilCandidatDbContext>().Database.Migrate();
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

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(Spectrometre.Host.Client._Imports).Assembly,
        typeof(Spectrometre.Modules.ProfilCandidat.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.ProfilEntreprise.ServiceCollectionExtensions).Assembly,
        typeof(Spectrometre.Modules.Compatibilite.ServiceCollectionExtensions).Assembly);

app.Run();
