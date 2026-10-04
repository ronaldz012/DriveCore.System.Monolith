using Common.Domain;
using Module.Inventory.Domain.Organization;
using Module.Inventory.Domain.Products;
using Module.Inventory.Domain.Shared.Base;

namespace Module.Inventory.Domain.Receptions;

public class StockReception : Params, IMustHaveTenant
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid BranchId { get; set; }
    public Guid ProviderId { get; set; }
    public int Number { get; set; }
    public DateTime ReceivedAt { get; set; }
    public ReceptionStatus Status { get; set; } = ReceptionStatus.Confirmed;
    public string? Notes { get; set; }

    public Provider Provider { get; set; } = null!;
    public ICollection<StockReceptionItem> Items { get; set; } = new List<StockReceptionItem>();

    public static StockReception Create(Guid branchId, Guid userId, string userName, string? notes, Guid providerId)
    {
        return new StockReception
        {
            Id = Guid.NewGuid(),
            BranchId = branchId,
            ProviderId = providerId,
            Notes = notes,
            ReceivedAt = DateTime.UtcNow,
            Status = ReceptionStatus.Confirmed,
            CreatedBy = userId,
            CreatedByName = userName
        };
    }

    public void AddExistingVariant(Guid variantId, Guid userId, string userName, int quantity, decimal unitCost)
    {
        Items.Add(new StockReceptionItem
        {
            ProductVariantId = variantId,
            QuantityReceived = quantity,
            UnitCost = unitCost,
            CreatedBy = userId,
            CreatedByName = userName
        });
    }
}

public class StockReceptionItem : Params, IMustHaveTenant
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid StockReceptionId { get; set; }
    public Guid ProductVariantId { get; set; }
    public int QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }

    public StockReception StockReception { get; set; } = null!;
    public ProductVariant ProductVariant { get; set; } = null!;
}

public enum ReceptionStatus
{
    Draft = 0,
    Confirmed = 1,
    Rejected = 2,
    Reverted = 3,
}

