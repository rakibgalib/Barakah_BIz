using Barakah.PharmacyService.Entities;
using Microsoft.EntityFrameworkCore;

namespace Barakah.PharmacyService.Data;

public class PharmacyDbContext(DbContextOptions<PharmacyDbContext> options) : DbContext(options)
{
    public DbSet<Prescription> Prescriptions => Set<Prescription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // No explicit schema here — resolved per-request via search_path
        // (Barakah.Persistence.TenantSchemaExtensions.UseTenantSchemaAsync).
        modelBuilder.Entity<Prescription>(e =>
        {
            e.ToTable("prescriptions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.PatientId).HasColumnName("patient_id");
            e.Property(x => x.PatientName).HasColumnName("patient_name");
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.MedicationName).HasColumnName("medication_name");
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.PrescribedBy).HasColumnName("prescribed_by");
            e.Property(x => x.RefillsAllowed).HasColumnName("refills_allowed");
            e.Property(x => x.RefillsUsed).HasColumnName("refills_used");
            e.Property(x => x.IsControlledSubstance).HasColumnName("is_controlled_substance");
            e.Property(x => x.RequiresPharmacistApproval).HasColumnName("requires_pharmacist_approval");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            e.HasIndex(x => x.PatientId);
        });
    }
}
