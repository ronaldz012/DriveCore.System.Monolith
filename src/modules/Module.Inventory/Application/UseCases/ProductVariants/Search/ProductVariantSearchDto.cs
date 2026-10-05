using Module.Inventory.Domain.Products;

namespace Module.Inventory.Application.UseCases.ProductVariants.Search;

public class ProductVariantSearchDto
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public Guid ColorId { get; set; }
    public string ColorName { get; set; } = string.Empty;
    public Guid SizeId { get; set; }
    public string Size { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int AvailableStockInBranch { get; set; }
}