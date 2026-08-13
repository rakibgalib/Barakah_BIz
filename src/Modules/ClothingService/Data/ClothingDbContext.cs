using Barakah.ClothingService.Entities;
using Microsoft.EntityFrameworkCore;

namespace Barakah.ClothingService.Data;

public class ClothingDbContext(DbContextOptions<ClothingDbContext> options) : DbContext(options)
{
    public DbSet<SustainabilityRating> SustainabilityRatings => Set<SustainabilityRating>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // No explicit schema here — resolved per-request via search_path
        // (Barakah.Persistence.TenantSchemaExtensions.UseTenantSchemaAsync).
        modelBuilder.Entity<SustainabilityRating>(e =>
        {
            e.ToTable("sustainability_ratings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.Score).HasColumnName("score");
            e.Property(x => x.Certifications).HasColumnName("certifications");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(x => x.ProductId).IsUnique();
        });
    }
}
