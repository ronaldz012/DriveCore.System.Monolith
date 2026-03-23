namespace Inventory.Contracts.Dtos.Products;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;

    public List<ProductVariantDto> ProductVariants { get; set; } = new List<ProductVariantDto>();
}

public class ProductVariantDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal Price { get; set; }
}