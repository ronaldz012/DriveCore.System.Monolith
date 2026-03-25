using Inventory.Contracts.Dtos.Products;
using Inventory.Data.Entities.Receptions;
using Inventory.Data.Persistence;

namespace Inventory.Contracts.Dtos.Receptions;

public class StockReceptionDetailDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public DateTime ReceivedAt { get; set; }
    public string? Notes { get; set; }
    public ReceptionStatus Status { get; set; }
    public decimal TotalCost { get; set; }
    public List<StockReceptionItemDetailDto> Items { get; set; } = new();
}

public class StockReceptionItemDetailDto
{
    public int Id { get; set; }
    public int ProductVariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantDescription { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Subtotal { get; set; }
}