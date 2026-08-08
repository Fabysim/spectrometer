using Microsoft.EntityFrameworkCore;
using Spectrometre.Modules.Entretien.Entities;

namespace Spectrometre.Modules.Entretien.Data;

/// <summary>
/// Catalogue partagé de questions d'entrevue — schéma <c>public</c> uniquement (pas tenant-scopé),
/// comme dans le MVP. Les réponses vivent dans <see cref="EntretienDbContext"/> (schéma entreprise).
/// </summary>
public sealed class EntretienCatalogDbContext(DbContextOptions<EntretienCatalogDbContext> options)
    : DbContext(options)
{
    public DbSet<InterviewQuestionCategory> InterviewQuestionCategories => Set<InterviewQuestionCategory>();
    public DbSet<InterviewQuestionSubCategory> InterviewQuestionSubCategories => Set<InterviewQuestionSubCategory>();
    public DbSet<InterviewQuestion> InterviewQuestions => Set<InterviewQuestion>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("public");

        builder.Entity<InterviewQuestionCategory>(e =>
        {
            e.ToTable("InterviewQuestionCategories", "public");
            e.HasIndex(c => c.SeedKey).IsUnique().HasFilter("\"SeedKey\" IS NOT NULL");
            e.Property(c => c.Name).HasMaxLength(300);
            e.Property(c => c.SeedKey).HasMaxLength(128);
        });

        builder.Entity<InterviewQuestionSubCategory>(e =>
        {
            e.ToTable("InterviewQuestionSubCategories", "public");
            e.HasIndex(s => s.SeedKey).IsUnique().HasFilter("\"SeedKey\" IS NOT NULL");
            e.Property(s => s.Name).HasMaxLength(300);
            e.Property(s => s.SeedKey).HasMaxLength(128);
            e.HasOne(s => s.Category)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(s => s.InterviewQuestionCategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<InterviewQuestion>(e =>
        {
            e.ToTable("InterviewQuestions", "public");
            e.HasIndex(q => q.SeedKey).IsUnique().HasFilter("\"SeedKey\" IS NOT NULL");
            e.Property(q => q.Text).HasMaxLength(2000);
            e.Property(q => q.SeedKey).HasMaxLength(128);
            e.HasOne(q => q.SubCategory)
                .WithMany(s => s.Questions)
                .HasForeignKey(q => q.InterviewQuestionSubCategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
