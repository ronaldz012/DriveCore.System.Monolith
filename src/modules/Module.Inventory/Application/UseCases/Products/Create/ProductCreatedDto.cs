using Module.Inventory.Domain.Products;

namespace Module.Inventory.Application.UseCases.Products.Create;

public class ProductCreatedDto
{
    public required Guid Id {get ;set;}
    public required string InternalCode {get; set;} = string.Empty;
    public required string Name {get;set;} = string.Empty;
    public required string BrandName  {get;set;}
    public required string CategoryName { get; set; }
    public Gender Gender {get;set;}
    public string Description {get;set;} = string.Empty;

    public bool IsActive { get; set; } = true;

    public List<ProductVariantsCreated> Variants { get; set; } = [];


}

public class ProductVariantsCreated
{
    public Guid Id {get ;set;}
    public string Sku {get; set;} = string.Empty;
    public Guid SizeId {get;set;}
    public string Size {get; set;} = string.Empty;
    public int sizeOrder {get;set;} 
    public Guid ColorId {get;set;} 
    public string ColorName {get; set;} = string.Empty;
    public decimal Price {get;set;}
}
