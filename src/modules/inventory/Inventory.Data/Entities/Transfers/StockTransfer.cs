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
    public void Accept(int userId, string? notes)
    {
        if (Status != TransferStatus.Pending)
            throw new InvalidOperationException("Only pending transfers can be accepted");

        Status = TransferStatus.Completed;
        AcceptedByUserId = userId;
        ResolvedAt = DateTime.UtcNow;
        if (notes != null) Notes = notes;
    }

    public void Reject(int userId, string? notes)
    {
        if (Status != TransferStatus.Pending)
            throw new InvalidOperationException("Only pending transfers can be rejected");

        Status = TransferStatus.Rejected;
        AcceptedByUserId = userId;
        ResolvedAt = DateTime.UtcNow;
        if (notes != null) Notes = notes;
    }

    public void Cancel(int userId)
    {
        if (Status != TransferStatus.Pending)
            throw new InvalidOperationException("Only pending transfers can be cancelled");

        Status = TransferStatus.Cancelled;
        ResolvedAt = DateTime.UtcNow;
    }
}


public enum TransferStatus
{
    Pending,
    Transit,
    Completed,
    Rejected,
    Cancelled
}