using Module.Inventory.Domain.Transfers;

namespace Module.Inventory.Application.UseCases.Transfers.GetById;

public class StockTransferDetailDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public TransferDirection Direction { get; set; }
    public string FromBranchName { get; set; } = string.Empty;
    public string ToBranchName { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public string? ResolverName { get; set; } = null;
    public TransferStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public List<StockTransferItemDetailDto> Items { get; set; } = [];
}

public class StockTransferItemDetailDto
{
    public Guid ProductVariantId { get; set; }
    public Guid ProductId {get; set;}
    public string BrandName {get;set;} = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string VariantDescription { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int QuantityRequested { get; set; }
}