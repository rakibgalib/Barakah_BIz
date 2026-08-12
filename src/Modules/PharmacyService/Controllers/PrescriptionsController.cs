using Barakah.Persistence;
using Barakah.PharmacyService.Data;
using Barakah.PharmacyService.Dtos;
using Barakah.PharmacyService.Entities;
using Barakah.TenantContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Barakah.PharmacyService.Controllers;

[ApiController]
[Route("api/prescriptions")]
public class PrescriptionsController(PharmacyDbContext db, ITenantContext tenant, IOptions<PharmacyOptions> options) : ControllerBase
{
    private readonly PharmacyOptions _options = options.Value;

    [HttpPost]
    public async Task<IActionResult> Create(CreatePrescriptionRequest request, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant) return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        if (request.RefillsAllowed > _options.MaxPrescriptionRefills)
        {
            return BadRequest(new { error = $"RefillsAllowed cannot exceed the configured maximum of {_options.MaxPrescriptionRefills}." });
        }

        var requiresApproval = request.IsControlledSubstance && _options.RequiresPharmacistApproval;

        var prescription = new Prescription
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.TenantId!.Value,
            PatientId = request.PatientId,
            PatientName = request.PatientName,
            ProductId = request.ProductId,
            MedicationName = request.MedicationName,
            Quantity = request.Quantity,
            PrescribedBy = request.PrescribedBy,
            RefillsAllowed = request.RefillsAllowed,
            RefillsUsed = 0,
            IsControlledSubstance = request.IsControlledSubstance,
            RequiresPharmacistApproval = requiresApproval,
            Status = requiresApproval ? "PendingApproval" : "Active",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = request.ExpiresAt,
        };

        db.Prescriptions.Add(prescription);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = prescription.Id }, ToResponse(prescription));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant) return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var prescription = await db.Prescriptions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        return prescription is null ? NotFound() : Ok(ToResponse(prescription));
    }

    // "Patient History" feature: complete medication record for one patient.
    [HttpGet("patient/{patientId}")]
    public async Task<IActionResult> GetByPatient(string patientId, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant) return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var prescriptions = await db.Prescriptions
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
        return Ok(prescriptions.Select(ToResponse));
    }

    // "Expiry Alerts" feature: prescriptions expiring within the configured warning window.
    [HttpGet("expiring")]
    public async Task<IActionResult> GetExpiring(CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant) return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var cutoff = DateTime.UtcNow.AddDays(_options.ExpiryWarningDays);
        var prescriptions = await db.Prescriptions
            .Where(p => p.Status == "Active" && p.ExpiresAt <= cutoff)
            .OrderBy(p => p.ExpiresAt)
            .ToListAsync(cancellationToken);
        return Ok(prescriptions.Select(ToResponse));
    }

    [HttpPost("{id:guid}/refill")]
    public async Task<IActionResult> Refill(Guid id, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant) return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var prescription = await db.Prescriptions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (prescription is null) return NotFound();
        if (prescription.RequiresPharmacistApproval && prescription.Status == "PendingApproval")
        {
            return BadRequest(new { error = "Prescription is pending pharmacist approval and cannot be refilled yet." });
        }
        if (prescription.Status != "Active")
        {
            return BadRequest(new { error = $"Prescription is not active (status: {prescription.Status})." });
        }
        if (prescription.ExpiresAt < DateTime.UtcNow)
        {
            prescription.Status = "Expired";
            await db.SaveChangesAsync(cancellationToken);
            return BadRequest(new { error = "Prescription has expired." });
        }
        if (prescription.RefillsUsed >= prescription.RefillsAllowed)
        {
            return BadRequest(new { error = "No refills remaining on this prescription." });
        }

        prescription.RefillsUsed++;
        if (prescription.RefillsUsed >= prescription.RefillsAllowed)
        {
            prescription.Status = "Fulfilled";
        }
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(prescription));
    }

    private static PrescriptionResponse ToResponse(Prescription p) => new(
        p.Id, p.TenantId, p.PatientId, p.PatientName, p.ProductId, p.MedicationName, p.Quantity,
        p.PrescribedBy, p.RefillsAllowed, p.RefillsUsed, p.IsControlledSubstance,
        p.RequiresPharmacistApproval, p.Status, p.CreatedAt, p.ExpiresAt);
}
