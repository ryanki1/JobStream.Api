using Microsoft.EntityFrameworkCore;
using JobStream.Api.Models;

namespace JobStream.Api.Data;

public class JobStreamDbContext : DbContext
{
    public JobStreamDbContext(DbContextOptions<JobStreamDbContext> options)
        : base(options)
    {
    }

    public DbSet<CompanyRegistration> CompanyRegistrations => Set<CompanyRegistration>();
    public DbSet<RegistrationDocument> RegistrationDocuments => Set<RegistrationDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure CompanyRegistration entity
        modelBuilder.Entity<CompanyRegistration>(entity =>
        {
            entity.ToTable("CompanyRegistrations");

            // Primary key
            entity.HasKey(e => e.Id);

            // Indexes for performance
            entity.HasIndex(e => e.CompanyEmail)
                .IsUnique();

            entity.HasIndex(e => e.Status);

            entity.HasIndex(e => e.CreatedAt);

            entity.HasIndex(e => e.EmailVerificationToken);

            entity.HasIndex(e => e.ExpiresAt);

            // Convert enum to string for storage
            entity.Property(e => e.Status)
                .HasConversion<string>();

            // Default values
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("datetime('now')");

            // Decimal precision for StakeAmount
            entity.Property(e => e.StakeAmount)
                .HasPrecision(18, 2);

            // Configure one-to-many relationship
            entity.HasMany(e => e.Documents)
                .WithOne(d => d.CompanyRegistration)
                .HasForeignKey(d => d.CompanyRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure RegistrationDocument entity
        modelBuilder.Entity<RegistrationDocument>(entity =>
        {
            entity.ToTable("RegistrationDocuments");

            // Primary key
            entity.HasKey(e => e.Id);

            // Indexes for performance
            entity.HasIndex(e => e.CompanyRegistrationId);

            entity.HasIndex(e => e.DocumentType);

            entity.HasIndex(e => e.UploadedAt);

            // Default values
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("datetime('now')");
        });
    }
}
