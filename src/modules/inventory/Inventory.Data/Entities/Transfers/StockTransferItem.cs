using Inventory.Data.Entities.Products;

namespace Inventory.Data.Entities.Transfers;

public class StockTransferItem
{
    public int Id { get; set; }
    public int TransferId { get; set; }
    public int ProductVariantId { get; set; }
    public int QuantityRequested { get; set; }

    public StockTransfer StockTransfer { get; set; } = null!;
    public ProductVariant ProductVariant { get; set; } = null!;
}