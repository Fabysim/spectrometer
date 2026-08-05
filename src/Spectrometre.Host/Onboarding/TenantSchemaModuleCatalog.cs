using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Compatibilite.Data;
using Spectrometre.Modules.Entretien.Data;
using Spectrometre.Modules.PostesRecrutement.Data;
using Spectrometre.Modules.ProfilEntreprise.Data;
using Spectrometre.Modules.SuiviEvolutif.Data;
using ProfilEntrepriseModule = Spectrometre.Modules.ProfilEntreprise.ServiceCollectionExtensions;
using CompatibiliteModule = Spectrometre.Modules.Compatibilite.ServiceCollectionExtensions;
using PostesRecrutementModule = Spectrometre.Modules.PostesRecrutement.ServiceCollectionExtensions;
using EntretienModule = Spectrometre.Modules.Entretien.ServiceCollectionExtensions;
using SuiviEvolutifModule = Spectrometre.Modules.SuiviEvolutif.ServiceCollectionExtensions;

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
        // Le côté CANDIDAT de SuiviEvolutif est un schéma fixe (comme ProfilCandidat), migré globalement
        // dans Program.cs — seul le côté ENTREPRISE (tenant-scopé) figure dans ce catalogue.
        new(SuiviEvolutifModule.Manifest.Code, async (sp, ct) => await sp.GetRequiredService<IDbContextFactory<SuiviEvolutifEntrepriseDbContext>>().CreateDbContextAsync(ct)),
        // Vivier : pas de schéma propre (lecture seule sur l'index partagé du noyau) — volontairement absent.
    ];
}
