using Inventory.Data.Entities.Products;
using Inventory.Data.Entities.Shared.Base;

namespace Inventory.Data.Entities.Inventory;

public class BranchInventory : Params
{
    public int Id { get; set; }
    public int BranchId { get; set; } //External ID
    public int ProductVariantId { get; set; }
    public int Stock { get; set; }
    public int MinStock { get; set; }
    
    public ProductVariant ProductVariant { get; set; } = null!;

}