using Module.Inventory.Domain.Products;

namespace Module.Inventory.Application.UseCases.Products.Search;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string InternalCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public Gender Gender { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt {get;set;}
    public int VariantsCount {get;set;}

    public List<ProductVariantDto> ProductVariants { get; set; } = new List<ProductVariantDto>();
}

public class ProductVariantDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;
    public int SizeOrder {get;set;}
    public Guid SizeId { get; set; }
    public Guid ColorId { get; set; }
    public string ColorName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal AverageCost { get; set; }
}