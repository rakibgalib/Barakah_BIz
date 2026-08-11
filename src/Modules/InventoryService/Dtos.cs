namespace Barakah.InventoryService.Dtos;

public record CreateInventoryItemRequest(Guid ProductId, Guid BranchId, int Quantity, int MinStockLevel, int MaxStockLevel);

public record AdjustInventoryRequest(int Delta, Guid? UpdatedBy);

public record InventoryItemResponse(
    Guid Id,
    Guid ProductId,
    Guid BranchId,
    int Quantity,
    int MinStockLevel,
    int MaxStockLevel,
    DateTime LastUpdated);
