using Barakah.InventoryService.Entities;
using Microsoft.EntityFrameworkCore;

namespace Barakah.InventoryService.Data;

public class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // No explicit schema here — resolved per-request via search_path
        // (Barakah.Persistence.TenantSchemaExtensions.UseTenantSchemaAsync).
        // product_id is a plain FK Guid — Inventory Service doesn't own the products
        // table, so there's no EF navigation to Catalog Service's Product entity.
        modelBuilder.Entity<InventoryItem>(e =>
        {
            e.ToTable("inventory");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.BranchId).HasColumnName("branch_id");
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.MinStockLevel).HasColumnName("min_stock_level");
            e.Property(x => x.MaxStockLevel).HasColumnName("max_stock_level");
            e.Property(x => x.LastUpdated).HasColumnName("last_updated");
            e.Property(x => x.UpdatedBy).HasColumnName("updated_by");
            e.HasIndex(x => new { x.TenantId, x.ProductId, x.BranchId }).IsUnique();
        });
    }
}
