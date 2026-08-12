namespace Barakah.RestaurantService;

/// <summary>
/// Mirrors the RestaurantExtension config block in docs/doc.html. Dynamic Pricing there is
/// rule-based (static multipliers), not AI — real-time demand forecasting would be, and is left
/// for the AI Integration phase. PeakHourStartUtc/EndUtc and IsRamadanPeriod are additions beyond
/// what doc.html specifies: it names the multipliers but not how "peak" or "Ramadan" get
/// determined, and real Hijri calendar calculation is out of scope here — IsRamadanPeriod is a
/// manually-toggled flag, not computed.
/// </summary>
public class RestaurantOptions
{
    public bool DynamicPricingEnabled { get; set; } = true;
    public decimal PeakHourPricing { get; set; } = 1.2m;
    public decimal OffPeakDiscount { get; set; } = 0.15m;
    public decimal RamadanPricing { get; set; } = 0.9m;
    public bool AllergenTracking { get; set; } = true;
    public int PeakHourStartUtc { get; set; } = 11;
    public int PeakHourEndUtc { get; set; } = 14;
    public bool IsRamadanPeriod { get; set; } = false;
}
