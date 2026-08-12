using Barakah.CatalogService.Entities;
using Microsoft.EntityFrameworkCore;

namespace Barakah.CatalogService.Data;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // No explicit schema here — resolved per-request via search_path
        // (Barakah.Persistence.TenantSchemaExtensions.UseTenantSchemaAsync).
        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.Sku).HasColumnName("sku");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.BasePrice).HasColumnName("base_price");
            e.Property(x => x.BusinessType).HasColumnName("business_type");
            e.Property(x => x.Category).HasColumnName("category");
            e.Property(x => x.Attributes).HasColumnName("attributes").HasColumnType("jsonb");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(x => new { x.TenantId, x.Sku }).IsUnique();
        });
    }
}
