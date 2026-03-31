namespace Inventory.Contracts.Dtos.Transfers;

public class CreateStockTransferDto
{
    public int ToBranchId { get; set; }
    public string? Notes { get; set; }
    public List<StockTransferItemDto> Items { get; set; } = new();
}

public class StockTransferItemDto
{
    public int ProductVariantId { get; set; }
    public int QuantityRequested { get; set; }
}