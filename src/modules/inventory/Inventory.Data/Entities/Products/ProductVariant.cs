using Inventory.Data.Entities.Inventory;
using Inventory.Data.Entities.Shared.Base;

namespace Inventory.Data.Entities.Products;

public class ProductVariant: Params
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Description { get; set; } = string.Empty; 
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal Price { get; set; }

    
    public Product Product { get; set; } = default!;
    public ICollection<BranchInventory> BranchInventories { get; set; } = new List<BranchInventory>();
    

}
