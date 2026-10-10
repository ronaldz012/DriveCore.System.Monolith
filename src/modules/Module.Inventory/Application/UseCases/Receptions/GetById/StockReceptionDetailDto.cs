using Module.Inventory.Domain.Receptions;

namespace Module.Inventory.Application.UseCases.Receptions.GetById;

public class StockReceptionDetailDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public Guid BranchId { get; set; }
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public string? Notes { get; set; }
    public ReceptionStatus Status { get; set; }
    public decimal TotalCost { get; set; }
    public List<StockReceptionItemDetailDto> Items { get; set; } = new();
}

public class StockReceptionItemDetailDto
{
    public Guid Id { get; set; }
    public Guid ProductVariantId { get; set; }
    public Guid ProductId {get;set;}
    public string BrandName {get;set;} = string.Empty;
    public required string Sku {get;set;} = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string VariantDescription { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Subtotal { get; set; }
}