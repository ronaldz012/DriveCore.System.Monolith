using Inventory.Data.Entities.Shared.Base;

namespace Inventory.Data.Entities.Transfers;

public class StockTransfer : Params
{
    public int Id { get; set; }
    public int FromBranchId { get; set; }
    public int ToBranchId { get; set; }
    public int RequestedByUserId { get; set; }  
    public int? AcceptedByUserId { get; set; }
    public TransferStatus Status { get; set; } = TransferStatus.Pending;
    public string? Notes { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public ICollection<StockTransferItem> Items { get; set; } = [];
}


public enum TransferStatus
{
    Pending,
    Transit,
    Completed,
    Rejected,
    Cancelled
}