using Barakah.OrderService.Entities;
using Microsoft.EntityFrameworkCore;

namespace Barakah.OrderService.Data;

public class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // No explicit schema here — resolved per-request via search_path
        // (Barakah.Persistence.TenantSchemaExtensions.UseTenantSchemaAsync).
        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.HasKey(x => x.Id);
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.BranchId).HasColumnName("branch_id");
            e.Property(x => x.OrderNumber).HasColumnName("order_number");
            e.Property(x => x.CustomerId).HasColumnName("customer_id");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.Subtotal).HasColumnName("subtotal");
            e.Property(x => x.Discount).HasColumnName("discount");
            e.Property(x => x.Tax).HasColumnName("tax");
            e.Property(x => x.Total).HasColumnName("total");
            e.Property(x => x.PaymentMethod).HasColumnName("payment_method");
            e.Property(x => x.PaymentStatus).HasColumnName("payment_status");
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(x => x.OrderNumber).IsUnique();
            e.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.OrderId);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.ToTable("order_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.UnitPrice).HasColumnName("unit_price");
            e.Property(x => x.TotalPrice).HasColumnName("total_price");
            e.Property(x => x.Discount).HasColumnName("discount");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });
    }
}
