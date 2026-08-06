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

    public DbSet<Cycle> Cycles => Set<Cycle>();
    public DbSet<TypeDeTemps> TypesDeTemps => Set<TypeDeTemps>();
    public DbSet<Activite> Activites => Set<Activite>();
    public DbSet<KanbanStatut> KanbanStatuts => Set<KanbanStatut>();
    public DbSet<ProfilPsychosocial> ProfilsPsychosociaux => Set<ProfilPsychosocial>();
    public DbSet<ReflexionConsciente> ReflexionsConscientes => Set<ReflexionConsciente>();
    public DbSet<Synthese> Syntheses => Set<Synthese>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(SchemaName);

        builder.Entity<Cycle>(e =>
        {
            e.HasIndex(c => new { c.UserId, c.NumeroCycle }).IsUnique();
            // Un seul cycle EnCours à la fois par utilisateur — index unique FILTRÉ (comme
            // IX_GdtCycles_UserId_EnCours dans mvp), pas une contrainte globale sur UserId seul : un
            // utilisateur peut avoir plusieurs cycles Termine dans son historique.
            e.HasIndex(c => c.UserId)
                .IsUnique()
                .HasFilter($"\"Statut\" = '{CycleStatuts.EnCours}'");
        });

        builder.Entity<TypeDeTemps>(e =>
        {
            // Scopé par cycle, pas par utilisateur seul (voir TypeDeTemps) : la clôture d'un cycle recopie
            // ses types de temps vers le nouveau, donc le même Cle existe légitimement dans plusieurs cycles.
            e.HasIndex(t => new { t.CycleId, t.Cle }).IsUnique();

            e.HasOne<Cycle>().WithMany().HasForeignKey(t => t.CycleId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Activite>(e =>
        {
            e.HasIndex(a => new { a.UserId, a.DateActivite, a.HeureDebut });

            e.HasOne<Cycle>().WithMany().HasForeignKey(a => a.CycleId).OnDelete(DeleteBehavior.Cascade);

            // FK réelle (pas une référence par identifiant façon inter-schéma) : TypeDeTemps et Activite
            // vivent dans le MÊME schéma fixe ici, contrairement à CandidateProfileId ailleurs dans la
            // solution qui traverse des schémas tenant différents. Restrict comme dans mvp : on ne supprime
            // pas un type de temps tant que des activités y sont rattachées.
            e.HasOne<TypeDeTemps>()
                .WithMany()
                .HasForeignKey(a => a.TypeDeTempsId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<KanbanStatut>(e =>
        {
            // Relation un-à-un avec Activite : une seule ligne de statut Kanban par activité.
            e.HasIndex(s => s.ActiviteId).IsUnique();

            e.HasOne<Activite>().WithMany().HasForeignKey(s => s.ActiviteId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ProfilPsychosocial>(e =>
        {
            // Un seul profil par cycle (comme IX_GdtProfilsPsychosocial_GdtCycleId dans mvp).
            e.HasIndex(p => p.CycleId).IsUnique();
            e.HasOne<Cycle>().WithMany().HasForeignKey(p => p.CycleId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ReflexionConsciente>(e =>
        {
            e.HasIndex(r => r.CycleId).IsUnique();
            e.HasOne<Cycle>().WithMany().HasForeignKey(r => r.CycleId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Synthese>(e =>
        {
            e.HasIndex(s => s.CycleId).IsUnique();
            e.HasOne<Cycle>().WithMany().HasForeignKey(s => s.CycleId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
