using Inventory.Data.Entities.Products;
using Inventory.Data.Entities.Shared.Base;

namespace Inventory.Data.Entities.Receptions;

public class StockReception : Params
{
    public int Id { get; set; }
    public int BranchId { get; set; } // External ID
    public DateTime ReceivedAt { get; set; }
    public ReceptionStatus Status { get; set; } = ReceptionStatus.Confirmed;
    public string? Notes { get; set; }

    public ICollection<StockReceptionItem> Items { get; set; } = new List<StockReceptionItem>();
}

public class StockReceptionItem : Params
{
    public int Id { get; set; }
    public int StockReceptionId { get; set; }
    public int ProductVariantId { get; set; }
    public int QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }

    public StockReception StockReception { get; set; } = null!;
    public ProductVariant ProductVariant { get; set; } = null!;
}

public enum ReceptionStatus
{
    Draft=0,
    Confirmed=1,
    Rejected=2
}