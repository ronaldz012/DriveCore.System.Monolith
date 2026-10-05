using Common.Utilities;
using Module.Inventory.Domain.Products;

namespace Module.Inventory.Application.UseCases.Products.Get;

public class ListProductRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string InternalCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty; // Para el chip gris
    public string BrandName { get; set; } = string.Empty;    // Para el título en negrita

    // Resúmenes para la metadata secundaria
    public int VariantsCount { get; set; }
    public decimal TotalStock { get; set; }
    public decimal BasePrice { get; set; } // Puede ser el BasePrice o el Min(Price) de variantes
    public bool IsActive { get; set; } = true;
}
public class ProductQueryDto : PaginationQueryDto
{
    public string? Filter { get; set; }
    public Guid? CategoryId { get; set; }
    public Gender? Gender { get; set; }
    public bool? LowStock { get; set; }
    public bool? IncludeInactive { get; set; }
    public ProductSortBy SortBy { get; set; } = ProductSortBy.CreatedAt;
    public bool? SortDescending { get; set; }
}

public enum ProductSortBy
{
    CreatedAt,
    Stock
}