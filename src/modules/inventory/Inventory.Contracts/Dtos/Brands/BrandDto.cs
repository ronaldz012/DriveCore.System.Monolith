using Shared.Extensions;

namespace Inventory.Contracts.Dtos.Brands;

public class BrandDto
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class QueryBrandDto : GenericPaginationQueryDto
{
    
}