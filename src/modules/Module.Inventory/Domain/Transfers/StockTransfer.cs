using Common.Domain;
using Module.Inventory.Domain.Inventory;
using Module.Inventory.Domain.Shared.Base;

namespace Module.Inventory.Domain.Transfers;

public class StockTransfer : Params, IMustHaveTenant
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FromBranchId { get; set; }
    public Guid ToBranchId { get; set; }
    public int Number { get; set; }
    public Guid RequestedByUserId { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public TransferStatus Status { get; set; } = TransferStatus.Pending;
    public string? Notes { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public ICollection<StockMovement> StockMovements { get; set; } = [];
    public ICollection<StockTransferItem> Items { get; set; } = [];
    public void Accept(Guid userId, string? userName, string? notes)
    {
        if (Status != TransferStatus.Pending)
            throw new InvalidOperationException("Only pending transfers can be accepted");

        Status = TransferStatus.Completed;
        ResolvedByUserId = userId;
        ResolvedAt = DateTime.UtcNow;
        UpdatedBy = userId;
        UpdatedByName = userName;
        UpdatedAt = DateTime.UtcNow;
        if (notes != null) Notes = notes;
    }

    public void Reject(Guid userId, string? userName, string? notes)
    {
        if (Status != TransferStatus.Pending)
            throw new InvalidOperationException("Only pending transfers can be rejected");

        Status = TransferStatus.Rejected;
        ResolvedByUserId = userId;
        ResolvedAt = DateTime.UtcNow;
        UpdatedBy = userId;
        UpdatedByName = userName;
        UpdatedAt = DateTime.UtcNow;
        if (notes != null) Notes = notes;
    }

    public void Cancel(Guid userId, string? userName)
    {
        if (Status != TransferStatus.Pending)
            throw new InvalidOperationException("Only pending transfers can be cancelled");

        Status = TransferStatus.Cancelled;
        ResolvedAt = DateTime.UtcNow;
        UpdatedBy = userId;
        UpdatedAt = DateTime.UtcNow;
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