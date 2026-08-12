using Barakah.RestaurantService.Entities;
using Microsoft.EntityFrameworkCore;

namespace Barakah.RestaurantService.Data;

public class RestaurantDbContext(DbContextOptions<RestaurantDbContext> options) : DbContext(options)
{
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // No explicit schema here — resolved per-request via search_path
        // (Barakah.Persistence.TenantSchemaExtensions.UseTenantSchemaAsync).
        modelBuilder.Entity<MenuItem>(e =>
        {
            e.ToTable("menu_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.BasePrice).HasColumnName("base_price");
            e.Property(x => x.Category).HasColumnName("category");
            e.Property(x => x.AllergenTags).HasColumnName("allergen_tags");
            e.Property(x => x.IsAvailable).HasColumnName("is_available");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });
    }
}
