using Microsoft.EntityFrameworkCore;
using Spectrometre.Modules.GestionDuTemps.Entities;

namespace Spectrometre.Modules.GestionDuTemps.Data;

/// <summary>
/// Schéma fixe <c>gestion_du_temps</c> — non tenant-scopé, même principe que <c>ProfilCandidatDbContext</c> :
/// ce module est personnel à l'utilisateur (n'importe quel profil), pas une donnée d'entreprise. Toutes les
/// données sont scopées par <c>UserId</c> (Identity), jamais par entreprise seule — voir
/// <see cref="TypeDeTemps.CompanyId"/>/<see cref="Activite.CompanyId"/> pour le marquage optionnel.
/// </summary>
public sealed class GestionDuTempsDbContext(DbContextOptions<GestionDuTempsDbContext> options) : DbContext(options)
{
    public const string SchemaName = "gestion_du_temps";

    public DbSet<TypeDeTemps> TypesDeTemps => Set<TypeDeTemps>();
    public DbSet<Activite> Activites => Set<Activite>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(SchemaName);

        builder.Entity<TypeDeTemps>(e =>
        {
            e.HasIndex(t => new { t.UserId, t.Cle }).IsUnique();
        });

        builder.Entity<Activite>(e =>
        {
            e.HasIndex(a => new { a.UserId, a.DateActivite, a.HeureDebut });

            // FK réelle (pas une référence par identifiant façon inter-schéma) : TypeDeTemps et Activite
            // vivent dans le MÊME schéma fixe ici, contrairement à CandidateProfileId ailleurs dans la
            // solution qui traverse des schémas tenant différents. Restrict comme dans mvp : on ne supprime
            // pas un type de temps tant que des activités y sont rattachées.
            e.HasOne<TypeDeTemps>()
                .WithMany()
                .HasForeignKey(a => a.TypeDeTempsId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
