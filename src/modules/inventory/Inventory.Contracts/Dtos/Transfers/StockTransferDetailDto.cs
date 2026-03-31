using Inventory.Data.Entities.Transfers;

namespace Inventory.Contracts.Dtos.Transfers;

public class StockTransferDetailDto
{
    public int Id { get; set; }
    public int FromBranchId { get; set; }
    public int ToBranchId { get; set; }
    public int RequestedByUserId { get; set; }
    public int? AcceptedByUserId { get; set; }
    public TransferStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public List<StockTransferItemDetailDto> Items { get; set; } = new();
}

public class StockTransferItemDetailDto
{
    public int ProductVariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantDescription { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int QuantityRequested { get; set; }
}