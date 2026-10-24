using Common.Domain;
using Module.Inventory.Domain.Inventory;
using Module.Inventory.Domain.Receptions;
using Module.Inventory.Domain.Shared.Base;
using Module.Inventory.Domain.Transfers;

namespace Module.Inventory.Domain.Products;

public class ProductVariant: Params, IMustHaveTenant
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public Guid TenantId { get; set; }
    public Guid ColorId { get; set; }
    public Guid SizeId { get; set; }
    public decimal AverageCost { get; set; }

    public Color Color { get; set; } = null!;
    public Size Size { get; set; } = null!;
    
    public Product Product { get; set; } = default!;
    public ICollection<BranchInventory> BranchInventories { get; set; } = new List<BranchInventory>();
    public ICollection<StockReceptionItem>  StockReceptionItems { get; set; } = new List<StockReceptionItem>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public ICollection<StockTransferItem> TransferItems { get; set; } = new List<StockTransferItem>();

    public static string BuildDisplayName(
        string categoryName, string brandName, string productName, string colorName, string sizeName)
        => $"{categoryName} {brandName} {productName} - {colorName} / {sizeName}";

    public static ProductVariant Create(Guid productId, Guid colorId, Guid sizeId, decimal price, string sku, Guid tenantId, Guid createdBy, string createdByName)
    {
        return new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ColorId = colorId,
            SizeId = sizeId,
            Price = price,
            Sku = sku,
            TenantId = tenantId,
            CreatedBy = createdBy,
            CreatedByName = createdByName
        };
    }

    public bool HasSufficientStock(int quantity, Guid branchId)
    {
        var branchInventory = BranchInventories.FirstOrDefault(bi => bi.BranchId == branchId);
        return branchInventory != null && branchInventory.Stock >= quantity;
    }

    public void SellStock(int quantity, Guid branchId, Guid userId, string userName, Guid referenceId, string? notes = null)
    {
        var branchInventory = BranchInventories.FirstOrDefault(bi => bi.BranchId == branchId);
        if (branchInventory == null)
            throw new InvalidOperationException($"No existe registro de inventario para la sucursal {branchId}");

        if (branchInventory.Stock < quantity)
            throw new InvalidOperationException($"Stock insuficiente para {Sku}");

        var stockBefore = branchInventory.Stock;
        branchInventory.Stock -= quantity;
        var stockAfter = branchInventory.Stock;
        StockMovements.Add(StockMovement.CreateSale(branchId, Id, userId, userName, quantity, referenceId, AverageCost, stockBefore, stockAfter, notes));
    }

    public void RegisterPurchase(int quantity, decimal unitCost)
    {
        var totalStock = BranchInventories.Sum(bi => bi.Stock);
        var totalValue = AverageCost * totalStock + unitCost * quantity;
        var newTotal = totalStock + quantity;
        AverageCost = newTotal == 0 ? 0 : totalValue / newTotal;
    }

    public void RevertPurchase(int quantity, decimal originalUnitCost)
    {
        var currentStock = BranchInventories.Sum(bi => bi.Stock);
        var newStock = currentStock - quantity;
        if (newStock <= 0)
        {
            AverageCost = 0;
            return;
        }

        var currentValue = AverageCost * currentStock;
        var revertedValue = originalUnitCost * quantity;
        var newValue = currentValue - revertedValue;
        if (newValue < 0) newValue = 0;
        AverageCost = newValue / newStock;
    }


    public void AddQuantity(int quantity, Guid branchId, Guid userId, string userName)
    {
        var branchInventory = BranchInventories.FirstOrDefault(bi => bi.BranchId == branchId);
        if (branchInventory == null)
        {
            branchInventory = new BranchInventory
            {
                BranchId = branchId,
                ProductVariantId = Id,
                Stock = 0,
                CreatedBy = userId,
                CreatedByName = userName
            };
            BranchInventories.Add(branchInventory);
        }
        branchInventory.Stock += quantity;
    }

    public void CorrectQuantity(int quantity, Guid branchId, Guid userId, string userName)
    {
        var branchInventory = BranchInventories.FirstOrDefault(bi => bi.BranchId == branchId);
        if (branchInventory == null)
        {
            branchInventory = new BranchInventory
            {
                BranchId = branchId,
                ProductVariantId = Id,
                Stock = quantity,
                CreatedBy = userId,
                CreatedByName = userName
            };
            BranchInventories.Add(branchInventory);
        }
        branchInventory.Stock = quantity;
    }
    public void RemoveQuantity(int quantity, Guid branchId)
    {
        var branchInventory = BranchInventories.FirstOrDefault(bi => bi.BranchId == branchId);
        
        if (branchInventory == null)
            throw new InvalidOperationException($"No existe registro de inventario para la sucursal {branchId}");

        if (branchInventory.Stock < quantity)
            throw new InvalidOperationException($"Cantidad solicitada para {Sku} excede el stock disponible ({branchInventory.Stock})");

        branchInventory.Stock -= quantity;
    }
    public int GetStockByBranch(Guid branchId) 
        => BranchInventories.FirstOrDefault(bi => bi.BranchId == branchId)?.Stock ?? 0;

    public void SoftDelete(Guid userId, string deletedByName)
    {
       DeletedAt = DateTime.UtcNow;
       DeletedBy = userId;
       DeletedByName = deletedByName;
    }
}
