using System.ComponentModel.DataAnnotations;
using Shared.Extensions;

namespace Inventory.Contracts.Dtos.Products;

public class ListProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public decimal Stock { get; set; }
    public string InternalCode { get; set; } = string.Empty;
}
public class ProductQueryDto : GenericPaginationQueryDto
{
    public int? CategoryId { get; set; }
}