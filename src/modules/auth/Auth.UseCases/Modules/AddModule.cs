using Auth.Data.Entities;
using Auth.Data.Persistence;
using Auth.Dtos.Modules;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Shared.Result;

namespace Auth.UseCases.Modules;

public class AddModule(AuthDbContext dbContext, IMapper mapper)
{

    public async Task<Result<int>> Execute(CreateModuleDto dto)
    {
        var exists = await dbContext.Modules
            .AnyAsync(m => m.Name == dto.Name);
        if (exists) return new Error("DUPLICATE", "Ya existe un módulo con ese nombre");

        var module = mapper.Map<Module>(dto);
        dbContext.Modules.Add(module);
        await dbContext.SaveChangesAsync();
        return module.Id;
    }
}
