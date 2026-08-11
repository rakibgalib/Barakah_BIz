using Barakah.TenantService.Entities;
using Microsoft.EntityFrameworkCore;

namespace Barakah.TenantService.Data;

public class TenantDbContext(DbContextOptions<TenantDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(e =>
        {
            e.ToTable("tenants", "public");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.Subdomain).HasColumnName("subdomain");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.Tier).HasColumnName("tier");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.StripeCustomerId).HasColumnName("stripe_customer_id");
            e.Property(x => x.StripeSubscriptionId).HasColumnName("stripe_subscription_id");
            e.Property(x => x.SubscriptionStart).HasColumnName("subscription_start");
            e.Property(x => x.SubscriptionEnd).HasColumnName("subscription_end");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(x => x.Subdomain).IsUnique();
        });
    }
}
