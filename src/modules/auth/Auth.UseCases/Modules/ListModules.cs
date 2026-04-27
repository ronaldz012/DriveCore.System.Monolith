using Auth.Contracts.Dtos.Modules;
using Auth.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Result;

namespace Auth.UseCases.Modules;

public class ListModules(AuthDbContext context)
{
    public async Task<Result<List<ModuleDto>> >Execute()
    {
        return await context.Modules.Select(x => new ModuleDto()
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            IsEnabled = x.IsEnabled,
            Icon = x.Icon,
        }).ToListAsync();
    }
}