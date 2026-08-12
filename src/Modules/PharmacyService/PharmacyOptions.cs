namespace Barakah.PharmacyService;

/// <summary>
/// Mirrors the PharmacyExtension config block in docs/doc.html — the tunable rules that give the
/// "Pharmacy Extension" real behavior beyond plain CRUD (refill limits, expiry warnings,
/// controlled-substance approval gating). EnableDrugInteractions is deliberately not modeled here:
/// a real interaction checker needs a drug-interaction knowledge base / AI reasoning, which
/// doc.html scopes to the project's AI Integration phase, not this slice.
/// </summary>
public class PharmacyOptions
{
    public int MaxPrescriptionRefills { get; set; } = 3;
    public bool ControlledSubstanceTracking { get; set; } = true;
    public bool RequiresPharmacistApproval { get; set; } = true;
    public int ExpiryWarningDays { get; set; } = 30;
}
