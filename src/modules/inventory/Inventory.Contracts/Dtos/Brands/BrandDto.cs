using Shared.Extensions;

namespace Inventory.Contracts.Dtos.Brands;

public class BrandDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class QueryBrandDto : GenericPaginationQueryDto
{
    
}