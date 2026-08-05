using Microsoft.EntityFrameworkCore;
using Spectrometre.Modules.SuiviEvolutif.Entities;

namespace Spectrometre.Modules.SuiviEvolutif.Data;

/// <summary>
/// Schéma fixe <c>suivi_evolutif_candidat</c> — non tenant-scopé, même raison que
/// <c>ProfilCandidatDbContext</c>. Enregistré via <c>IDbContextFactory</c> (voir ServiceCollectionExtensions) :
/// même si ce contexte n'est pas tenant-scopé, une instance UNIQUE partagée pour tout le circuit Blazor
/// Server serait utilisée concurremment par deux gestionnaires d'évènements qui se chevauchent — voir le
/// commentaire détaillé sur <c>CandidateProfileService</c> (module ProfilCandidat) pour l'historique de ce
/// choix, déjà appliqué là-bas pour la même raison.
/// </summary>
public sealed class SuiviEvolutifCandidatDbContext(DbContextOptions<SuiviEvolutifCandidatDbContext> options) : DbContext(options)
{
    public const string SchemaName = "suivi_evolutif_candidat";

    public DbSet<CandidateProfileChangeEntry> Entries => Set<CandidateProfileChangeEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(SchemaName);

        builder.Entity<CandidateProfileChangeEntry>(e =>
        {
            e.HasIndex(c => new { c.CandidateProfileId, c.Horodatage });
        });
    }
}
