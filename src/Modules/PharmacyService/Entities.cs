namespace Barakah.PharmacyService.Entities;

public class Prescription
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string PatientId { get; set; }
    public required string PatientName { get; set; }
    public Guid ProductId { get; set; }
    public required string MedicationName { get; set; }
    public int Quantity { get; set; }
    public required string PrescribedBy { get; set; }
    public int RefillsAllowed { get; set; }
    public int RefillsUsed { get; set; }
    public bool IsControlledSubstance { get; set; }
    public bool RequiresPharmacistApproval { get; set; }
    public required string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
