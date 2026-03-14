using Shared.Extensions;

namespace Inventory.Contracts.Dtos.Categories;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
public class CategoryQueryDto : GenericPaginationQueryDto
{

}