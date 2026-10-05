namespace Module.Inventory.Application.UseCases.Receptions.Create;

public class StockReceptionResultDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public Guid BranchId { get; set; }
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public string? Notes { get; set; }
    public List<StockReceptionItemResultDto> Items { get; set; } = new();
}

public class StockReceptionItemResultDto
{
    public Guid ProductVariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantDescription { get; set; } = string.Empty;
    public int QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }
}