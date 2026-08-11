using System.Text;
using Barakah.AnalyticsService.Data;
using Barakah.AnalyticsService.Dtos;
using Barakah.Persistence;
using Barakah.TenantContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Barakah.AnalyticsService.Controllers;

[ApiController]
[Route("api/analytics")]
public class AnalyticsController(AnalyticsReadDbContext db, ITenantContext tenant) : ControllerBase
{
    [HttpGet("sales-summary")]
    public async Task<ActionResult<SalesSummaryResponse>> GetSalesSummary(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant)
        {
            return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        }
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var effectiveTo = to ?? DateTime.UtcNow;
        var effectiveFrom = from ?? effectiveTo.AddDays(-30);

        var orders = await db.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= effectiveFrom && o.CreatedAt <= effectiveTo)
            .ToListAsync(cancellationToken);

        var totalOrders = orders.Count;
        var totalRevenue = orders.Sum(o => o.Total);
        var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0m;

        var totalPaymentsReceived = await db.Payments
            .AsNoTracking()
            .Where(p => p.Status == "completed" && p.CreatedAt >= effectiveFrom && p.CreatedAt <= effectiveTo)
            .SumAsync(p => p.Amount, cancellationToken);

        return Ok(new SalesSummaryResponse(
            totalOrders, totalRevenue, avgOrderValue, totalPaymentsReceived, effectiveFrom, effectiveTo));
    }

    [HttpGet("export/orders.csv")]
    public async Task<IActionResult> ExportOrdersCsv(CancellationToken cancellationToken)
    {
        if (!tenant.HasTenant)
        {
            return BadRequest(new { error = "Missing or unresolved X-Tenant-Subdomain header." });
        }
        await db.UseTenantSchemaAsync(tenant.TenantId!.Value, cancellationToken);

        var orders = await db.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Id,OrderNumber,Status,PaymentStatus,Total,CreatedAt");
        foreach (var order in orders)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(order.Id.ToString()),
                EscapeCsv(order.OrderNumber),
                EscapeCsv(order.Status),
                EscapeCsv(order.PaymentStatus),
                EscapeCsv(order.Total.ToString()),
                EscapeCsv(order.CreatedAt.ToString("O"))));
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "orders.csv");
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
