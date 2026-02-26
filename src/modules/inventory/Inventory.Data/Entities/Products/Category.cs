using Inventory.Data.Entities.Shared.Base;

namespace Inventory.Data.Entities.Products;

public class Category : Params
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ParentId { get; set; }
    public DateTime? DeletedAt { get; set; }
}
