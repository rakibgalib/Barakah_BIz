namespace Barakah.PharmacyService.Dtos;

public record CreatePrescriptionRequest(
    string PatientId,
    string PatientName,
    Guid ProductId,
    string MedicationName,
    int Quantity,
    string PrescribedBy,
    int RefillsAllowed,
    bool IsControlledSubstance,
    DateTime ExpiresAt);

public record PrescriptionResponse(
    Guid Id,
    Guid TenantId,
    string PatientId,
    string PatientName,
    Guid ProductId,
    string MedicationName,
    int Quantity,
    string PrescribedBy,
    int RefillsAllowed,
    int RefillsUsed,
    bool IsControlledSubstance,
    bool RequiresPharmacistApproval,
    string Status,
    DateTime CreatedAt,
    DateTime ExpiresAt);
