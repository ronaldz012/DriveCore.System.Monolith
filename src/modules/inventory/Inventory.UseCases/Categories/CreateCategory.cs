using Inventory.Contracts.Dtos.Categories;
using Inventory.Data.Entities.Products;
using Inventory.Data.Persistence;
using Shared.Result;

namespace Inventory.UseCases.Categories;

public class CreateCategory(InvDbContext context)
{
    public async Task<Result<bool>> Execute(CreateCategoryDto dto)
    {
        var category = new Category
        {
            Name = dto.Name,
        };
        context.Add(category);
        await context.SaveChangesAsync();
        return true;
    }
}