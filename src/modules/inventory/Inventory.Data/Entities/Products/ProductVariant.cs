using Inventory.Data.Entities.Inventory;
using Inventory.Data.Entities.Receptions;
using Inventory.Data.Entities.Shared.Base;
using Inventory.Data.Entities.Transfers;

namespace Inventory.Data.Entities.Products;

public class ProductVariant: Params
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty; 
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal Price { get; set; }

    
    public Product Product { get; set; } = default!;
    public ICollection<BranchInventory> BranchInventories { get; set; } = new List<BranchInventory>();
    public ICollection<StockReceptionItem>  StockReceptionItems { get; set; } = new List<StockReceptionItem>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public ICollection<StockTransferItem> TransferItems { get; set; } = new List<StockTransferItem>();
    public void UpdateQuantity(int quantity, int branchId)
    {
        var branchInventory = BranchInventories.FirstOrDefault(bi => bi.BranchId == branchId);
        if (branchInventory == null)
        {
            branchInventory = new BranchInventory
            {
                BranchId = branchId,
                ProductVariantId = Id,
                Stock = 0
            };
            BranchInventories.Add(branchInventory);
        }
        branchInventory.Stock += quantity;
    }
}
