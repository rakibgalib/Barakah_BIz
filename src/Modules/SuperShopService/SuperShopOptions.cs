namespace Barakah.SuperShopService;

/// <summary>
/// Mirrors the SuperShopExtension config block in docs/doc.html. AutoReorderEnabled is not
/// modeled here — automatic reordering needs demand-pattern intelligence, which doc.html scopes to
/// the AI Integration phase. BulkDiscountPercentage is an addition: doc.html names the quantity
/// threshold but not the discount rate that applies once it's crossed.
/// </summary>
public class SuperShopOptions
{
    public bool ExpiryMonitoringEnabled { get; set; } = true;
    public bool LoyaltyProgramEnabled { get; set; } = true;
    public int BulkDiscountThreshold { get; set; } = 10;
    public decimal BulkDiscountPercentage { get; set; } = 0.1m;
    public bool SupplierRatingEnabled { get; set; } = true;
    public int ExpiryWarningDays { get; set; } = 14;
}
