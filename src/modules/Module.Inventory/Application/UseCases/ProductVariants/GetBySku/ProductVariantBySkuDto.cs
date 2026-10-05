using Module.Inventory.Domain.Products;

namespace Module.Inventory.Application.UseCases.ProductVariants.GetBySku;

public class ProductVariantBySkuDto
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public Guid SizeId { get; set; }
    public Guid ColorId { get; set; }
    public string ColorName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal AverageCost { get; set; }
    public Guid BranchId { get; set; }
    public int AvailableStockInBranch { get; set; }

    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductDescription { get; set; } = string.Empty;
    public Gender Gender { get; set; } 
    public string BrandName  {get;set;}  = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}