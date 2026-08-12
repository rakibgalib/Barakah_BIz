using Barakah.PaymentService.Entities;
using Microsoft.EntityFrameworkCore;

namespace Barakah.PaymentService.Data;

public class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // No explicit schema here — resolved per-request via search_path
        // (Barakah.Persistence.TenantSchemaExtensions.UseTenantSchemaAsync).
        modelBuilder.Entity<Payment>(e =>
        {
            e.ToTable("payments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.Property(x => x.Amount).HasColumnName("amount");
            e.Property(x => x.Method).HasColumnName("method");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.TransactionReference).HasColumnName("transaction_reference");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(x => x.OrderId);
        });
    }
}
