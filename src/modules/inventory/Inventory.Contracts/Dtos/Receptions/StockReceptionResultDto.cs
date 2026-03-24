namespace Inventory.Contracts.Dtos.Receptions;

public class StockReceptionResultDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public DateTime ReceivedAt { get; set; }
    public string? Notes { get; set; }
    public List<StockReceptionItemResultDto> Items { get; set; } = new();
}

public class StockReceptionItemResultDto
{
    public int ProductVariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantDescription { get; set; } = string.Empty;
    public int QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }
}