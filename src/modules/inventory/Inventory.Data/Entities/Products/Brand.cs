using Inventory.Data.Entities.Shared.Base;

namespace Inventory.Data.Entities.Products;

public class Brand : Params
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}