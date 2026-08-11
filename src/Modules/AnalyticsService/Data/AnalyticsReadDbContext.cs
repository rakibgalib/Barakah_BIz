using Barakah.AnalyticsService.Entities;
using Microsoft.EntityFrameworkCore;

namespace Barakah.AnalyticsService.Data;

/// <summary>
/// Read-only projection of OrderService/PaymentService's tables — Analytics never writes here.
/// </summary>
public class AnalyticsReadDbContext(DbContextOptions<AnalyticsReadDbContext> options) : DbContext(options)
{
    public DbSet<OrderRecord> Orders => Set<OrderRecord>();
    public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // No explicit schema here — resolved per-request via search_path
        // (Barakah.Persistence.TenantSchemaExtensions.UseTenantSchemaAsync).
        modelBuilder.Entity<OrderRecord>(e =>
        {
            e.ToTable("orders");
            e.HasKey(x => x.Id);
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.BranchId).HasColumnName("branch_id");
            e.Property(x => x.OrderNumber).HasColumnName("order_number");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.PaymentStatus).HasColumnName("payment_status");
            e.Property(x => x.Total).HasColumnName("total");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<PaymentRecord>(e =>
        {
            e.ToTable("payments");
            e.HasKey(x => x.Id);
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.Property(x => x.Amount).HasColumnName("amount");
            e.Property(x => x.Method).HasColumnName("method");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });
    }
}
