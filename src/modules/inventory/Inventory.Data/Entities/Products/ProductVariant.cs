using Inventory.Data.Entities.Inventory;
using Inventory.Data.Entities.Shared.Base;

namespace Inventory.Data.Entities.Products;

public class ProductVariant: Params
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Description { get; set; } = string.Empty; // Descripcion del producto
    public string Barcode { get; set; } = string.Empty; // Codigo de barras del producto
    public Product Product { get; set; } = default!;
    public ICollection<ProductPricing> ProductPricings { get; set; } = new List<ProductPricing>();

}
