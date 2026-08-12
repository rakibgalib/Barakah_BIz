using Barakah.NotificationService.Entities;
using Microsoft.EntityFrameworkCore;

namespace Barakah.NotificationService.Data;

public class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // No explicit schema here — resolved per-request via search_path
        // (Barakah.Persistence.TenantSchemaExtensions.UseTenantSchemaAsync).
        modelBuilder.Entity<Notification>(e =>
        {
            e.ToTable("notifications");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.Property(x => x.Channel).HasColumnName("channel");
            e.Property(x => x.Recipient).HasColumnName("recipient");
            e.Property(x => x.Message).HasColumnName("message");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.SentAt).HasColumnName("sent_at");
            e.HasIndex(x => x.OrderId);
        });
    }
}
