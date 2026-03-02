using System;
using Auth.Contracts.Dtos.Modules;
using Auth.Data.Persistence;
using MapsterMapper;
using Shared.Result;

namespace Auth.UseCases.Menus;

public class GetMenu(AuthDbContext dbContext, IMapper mapper)
{
    public async Task<Result<MenuDto?>> Execute(int id)
    {
        var menu =  await dbContext.Menus.FindAsync(id);
        if (menu == null)
            return new Error("NOT_FOUND", "menu not found");

        return mapper.Map<MenuDto>(menu);
    }
}
