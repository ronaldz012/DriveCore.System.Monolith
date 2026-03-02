using Auth.Contracts.Dtos.Modules;
using Auth.Data.Entities;
using Auth.Data.Persistence;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Shared.Result;
namespace Auth.UseCases.Menus;

public class AddMenu(AuthDbContext dbContext, IMapper mapper)
{
    public async Task<Result<int>> Execute(CreateMenuDto dto)
    {
      
            var exists = await dbContext.Menus
                .AnyAsync(m => m.Name == dto.Name && m.ModuleId == dto.ModuleId);
            
            if (exists)
                return new Error("DUPLICATE", "Ya existe un menú con ese nombre en el mismo modulo");
            var menu = mapper.Map<Menu>(dto);
            dbContext.Menus.Add(menu);
            await dbContext.SaveChangesAsync();
            return menu.Id;
    }
}