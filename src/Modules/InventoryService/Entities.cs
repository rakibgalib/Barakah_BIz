namespace Barakah.InventoryService.Entities;

public class InventoryItem
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public Guid BranchId { get; set; }
    public int Quantity { get; set; }
    public int MinStockLevel { get; set; } = 5;
    public int MaxStockLevel { get; set; } = 1000;
    public DateTime LastUpdated { get; set; }
    public Guid? UpdatedBy { get; set; }
}
