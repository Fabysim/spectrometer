using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectrometre.Core;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;

// Outil exécuté HORS de l'application web (aucun endpoint HTTP, aucun écran, aucun déclenchement au
// démarrage de Program.cs de Spectrometre.Host) — seul moyen de créer/promouvoir le tout premier compte
// PlatformAdmin, indispensable pour que ce compte puisse ensuite en promouvoir d'autres depuis /admin
// (voir IAdminService.PromouvoirAsync, qui exige déjà d'être administrateur pour agir).
//
// Usage : dotnet run --project tools/Spectrometre.AdminBootstrap -- <email> <mot-de-passe> [--ConnectionStrings:DefaultConnection=...]
//   - Si le compte n'existe pas encore : il est créé (email confirmé d'office — ce chemin de création est
//     lui-même la preuve de propriété, comme pour une invitation acceptée) puis promu.
//   - Si le compte existe déjà : le mot de passe fourni est appliqué (reset), puis le compte est promu.
// La chaîne de connexion vient d'appsettings.json (placeholder committé), surchargeable
// par ConnectionStrings__DefaultConnection ou --ConnectionStrings:DefaultConnection=...
// Préférer tools/create-platform-admin.ps1 en local (mot de passe Postgres + defaults).
var positional = args.Where(a => !a.StartsWith('-') && !a.Contains('=')).ToArray();
if (positional.Length < 2)
{
    Console.Error.WriteLine("Usage : dotnet run --project tools/Spectrometre.AdminBootstrap -- <email> <mot-de-passe>");
    Console.Error.WriteLine("        (chaîne de connexion : appsettings.json, ou ConnectionStrings__DefaultConnection, ou --ConnectionStrings:DefaultConnection=...)");
    Console.Error.WriteLine("        Ou : powershell -File tools/create-platform-admin.ps1");
    return 1;
}

var email = positional[0];
var password = positional[1];

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole());
// AddSpectrometreCore enregistre AddDefaultTokenProviders() (jetons de confirmation email/réinitialisation
// de mot de passe), qui exige IDataProtectionProvider — fourni gratuitement par un hôte ASP.NET Core
// complet (voir Spectrometre.Host), absent ici puisque cet outil est un simple exécutable console.
services.AddDataProtection();
services.AddSpectrometreCore(configuration);
await using var provider = services.BuildServiceProvider();

using var scope = provider.CreateScope();
var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

// Idempotent — nécessaire ici en premier recours si l'application web n'a encore jamais démarré sur cette
// base (le seed équivalent dans Spectrometre.Host.Program suppose déjà une base migrée).
await coreDb.Database.MigrateAsync();

if (!await roleManager.RoleExistsAsync(PlatformRoles.Admin))
    await roleManager.CreateAsync(new IdentityRole(PlatformRoles.Admin));

var user = await userManager.FindByEmailAsync(email);
if (user is null)
{
    user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
    var createResult = await userManager.CreateAsync(user, password);
    if (!createResult.Succeeded)
    {
        foreach (var error in createResult.Errors)
            Console.Error.WriteLine($"Erreur : {error.Description}");
        return 1;
    }
    Console.WriteLine($"Compte créé : {email}");
}
else
{
    var token = await userManager.GeneratePasswordResetTokenAsync(user);
    var resetResult = await userManager.ResetPasswordAsync(user, token, password);
    if (!resetResult.Succeeded)
    {
        foreach (var error in resetResult.Errors)
            Console.Error.WriteLine($"Erreur reset mot de passe : {error.Description}");
        return 1;
    }

    if (!user.EmailConfirmed)
    {
        user.EmailConfirmed = true;
        await userManager.UpdateAsync(user);
    }

    Console.WriteLine($"Compte existant réutilisé : {email} (mot de passe mis à jour)");
}

if (await userManager.IsInRoleAsync(user, PlatformRoles.Admin))
{
    Console.WriteLine("Ce compte est déjà administrateur (PlatformAdmin).");
    return 0;
}

await userManager.AddToRoleAsync(user, PlatformRoles.Admin);
Console.WriteLine($"Compte promu administrateur (PlatformAdmin) : {email}");
return 0;
