using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Compatibilite.Data;
using Spectrometre.Modules.Entretien.Data;
using Spectrometre.Modules.PostesRecrutement.Data;
using Spectrometre.Modules.ProfilEntreprise.Data;
using ProfilEntrepriseModule = Spectrometre.Modules.ProfilEntreprise.ServiceCollectionExtensions;
using CompatibiliteModule = Spectrometre.Modules.Compatibilite.ServiceCollectionExtensions;
using PostesRecrutementModule = Spectrometre.Modules.PostesRecrutement.ServiceCollectionExtensions;
using EntretienModule = Spectrometre.Modules.Entretien.ServiceCollectionExtensions;

namespace Spectrometre.Host.Onboarding;

/// <summary>
/// Liste unique des modules tenant-scopés (schéma par entreprise) — seul endroit à modifier lorsqu'un
/// nouveau module tenant-scopé est ajouté. Utilisée à la fois par <see cref="CompanyOnboardingService"/>
/// (provisionnement d'une entreprise neuve) et par <c>TenantSchemaSynchronizer</c> (comblement rétroactif
/// des entreprises existantes au démarrage) — plus jamais besoin de dupliquer cette liste ou d'écrire un
/// script one-off à chaque nouveau module.
/// </summary>
public static class TenantSchemaModuleCatalog
{
    public static readonly IReadOnlyList<TenantSchemaModule> Modules =
    [
        new(ProfilEntrepriseModule.Manifest.Code, async (sp, ct) => await sp.GetRequiredService<IDbContextFactory<ProfilEntrepriseDbContext>>().CreateDbContextAsync(ct)),
        new(CompatibiliteModule.Manifest.Code, async (sp, ct) => await sp.GetRequiredService<IDbContextFactory<CompatibiliteDbContext>>().CreateDbContextAsync(ct)),
        new(PostesRecrutementModule.Manifest.Code, async (sp, ct) => await sp.GetRequiredService<IDbContextFactory<PostesRecrutementDbContext>>().CreateDbContextAsync(ct)),
        new(EntretienModule.Manifest.Code, async (sp, ct) => await sp.GetRequiredService<IDbContextFactory<EntretienDbContext>>().CreateDbContextAsync(ct)),
        // Vivier : pas de schéma propre (lecture seule sur l'index partagé du noyau) — volontairement absent.
        // SuiviEvolutif (ce cycle, côté entreprise) : ajouté ici une fois son DbContext créé.
    ];
}
