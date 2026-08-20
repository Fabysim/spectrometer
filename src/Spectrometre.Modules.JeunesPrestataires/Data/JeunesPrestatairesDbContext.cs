using Microsoft.EntityFrameworkCore;
using Spectrometre.Modules.JeunesPrestataires.Entities;

namespace Spectrometre.Modules.JeunesPrestataires.Data;

/// <summary>Schéma fixe <c>jeunes_prestataires</c> — non tenant-scopé.</summary>
public sealed class JeunesPrestatairesDbContext(DbContextOptions<JeunesPrestatairesDbContext> options) : DbContext(options)
{
    public const string SchemaName = "jeunes_prestataires";

    public DbSet<JeuneProfile> JeuneProfiles => Set<JeuneProfile>();
    public DbSet<InvitationJeunePrestataire> InvitationsJeunesPrestataires => Set<InvitationJeunePrestataire>();
    public DbSet<ConsentementParental> ConsentementsParentaux => Set<ConsentementParental>();
    public DbSet<AutoObservationReponse> AutoObservationReponses => Set<AutoObservationReponse>();
    public DbSet<AutoObservationSectionProgress> AutoObservationSectionProgress => Set<AutoObservationSectionProgress>();
    public DbSet<AutoObservationSyntheseGeneree> AutoObservationSynthesesGenerees => Set<AutoObservationSyntheseGeneree>();
    public DbSet<GrilleObservationEvaluation> GrilleObservationEvaluations => Set<GrilleObservationEvaluation>();
    public DbSet<GrilleObservationCritere> GrilleObservationCriteres => Set<GrilleObservationCritere>();
    public DbSet<GuideEntrevue> GuidesEntrevue => Set<GuideEntrevue>();
    public DbSet<GuideEntrevuePeurNote> GuideEntrevuePeurNotes => Set<GuideEntrevuePeurNote>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(SchemaName);

        builder.Entity<JeuneProfile>(e =>
        {
            e.HasIndex(p => p.UserId).IsUnique();
            e.HasIndex(p => p.InvitationId).IsUnique();
        });

        builder.Entity<InvitationJeunePrestataire>(e =>
        {
            e.HasIndex(d => d.InvitationId).IsUnique();
        });

        builder.Entity<ConsentementParental>(e =>
        {
            e.HasIndex(c => c.JeuneProfileId).IsUnique();
        });

        builder.Entity<AutoObservationReponse>(e =>
        {
            e.HasIndex(r => new { r.JeuneProfileId, r.QuestionKey }).IsUnique();
        });

        builder.Entity<AutoObservationSectionProgress>(e =>
        {
            e.HasIndex(p => new { p.JeuneProfileId, p.SectionKey }).IsUnique();
        });

        builder.Entity<AutoObservationSyntheseGeneree>(e =>
        {
            e.HasIndex(s => s.JeuneProfileId).IsUnique();
        });

        builder.Entity<GrilleObservationEvaluation>(e =>
        {
            e.HasIndex(x => x.JeuneProfileId);
            e.HasIndex(x => new { x.JeuneProfileId, x.EvalueeLe });
        });

        builder.Entity<GrilleObservationCritere>(e =>
        {
            e.HasIndex(c => new { c.EvaluationId, c.CritereKey }).IsUnique();
            e.HasOne(c => c.Evaluation)
                .WithMany(e => e.Criteres)
                .HasForeignKey(c => c.EvaluationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<GuideEntrevue>(e =>
        {
            e.HasIndex(g => g.JeuneProfileId).IsUnique();
        });

        builder.Entity<GuideEntrevuePeurNote>(e =>
        {
            e.HasIndex(n => new { n.GuideEntrevueId, n.PeurKey }).IsUnique();
            e.HasOne(n => n.GuideEntrevue)
                .WithMany(g => g.PeurNotes)
                .HasForeignKey(n => n.GuideEntrevueId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
