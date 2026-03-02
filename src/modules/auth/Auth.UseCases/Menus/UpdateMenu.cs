
using Auth.Contracts.Dtos.Modules;
using Auth.Data.Persistence;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.Result;

namespace Auth.UseCases.Menus;

public class UpdateMenu(AuthDbContext dbContext, IMapper mapper)
{
    public async Task<Result<CreateMenuDto>> Execute(UpdateMenuDto dto)
    {
        var menu = await dbContext.Menus.FindAsync(dto.Id);
        if (menu == null)
            return new Error("NOT_FOUND", "menu not found");

        var isDuplicate = await dbContext.Menus.AnyAsync(m => m.Id != dto.Id && m.Name == dto.Name && dto.ModuleId == menu.ModuleId);
        if (isDuplicate)
            return new Error("DUPLICATE", "a menu with the same name already exists in this module");

        mapper.Map(dto, menu);
        menu.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        return mapper.Map<CreateMenuDto>(menu);
    }

}
