using Inventory.Data.Entities.Products;

namespace Inventory.Contracts.Dtos.Products;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string InternalCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public Gender Gender { get; set; }
    

    public List<ProductVariantDto> ProductVariants { get; set; } = new List<ProductVariantDto>();
}

public class ProductVariantDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal Price { get; set; }
}