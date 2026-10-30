using Common.Domain;
using Module.Inventory.Domain.Inventory;
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

    public static StockReception Create(Guid branchId, Guid userId, string userName, string? notes, Guid providerId,
        ReceptionStatus status = ReceptionStatus.Confirmed)
    {
        return new StockReception
        {
            Id = Guid.NewGuid(),
            BranchId = branchId,
            ProviderId = providerId,
            Notes = notes,
            ReceivedAt = DateTime.UtcNow,
            Status = status,
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

    // TESTER: only answers whether every item has a valid purchase cost.
    public bool HasAllUnitCosts()
        => Items.Count > 0 && Items.All(i => i.UnitCost >= StockReceptionItem.MinUnitCost);

    // DOER: applies the reception to stock and returns the stock movements to persist.
    public List<StockMovement> Confirm(List<ProductVariant> variants, Guid branchId, Guid userId, string userName)
    {
        var variantMap = variants.ToDictionary(v => v.Id);
        var movements = new List<StockMovement>();

        foreach (var item in Items)
        {
            if (!variantMap.TryGetValue(item.ProductVariantId, out var variant))
                throw new InvalidOperationException($"Product variant {item.ProductVariantId} not found");

            var stockBefore = variant.GetStockByBranch(branchId);
            var stockAfter = stockBefore + item.QuantityReceived;

            variant.RegisterPurchase(item.QuantityReceived, item.UnitCost);
            variant.AddQuantity(item.QuantityReceived, branchId, userId, userName);

            movements.Add(StockMovement.CreateReception(
                branchId, variant.Id, userId, userName, item.QuantityReceived, Id, item.UnitCost, stockBefore, stockAfter));
        }

        Status = ReceptionStatus.Confirmed;
        ReceivedAt = DateTime.UtcNow;

        return movements;
    }
}

public class StockReceptionItem : Params, IMustHaveTenant
{
    public const decimal MinUnitCost = 0.5m;

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

