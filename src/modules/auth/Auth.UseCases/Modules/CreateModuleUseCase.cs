using Auth.Contracts.Dtos.Modules;
using Auth.Data.Entities;
using Auth.Data.Persistence;
using Shared.Result;

namespace Auth.UseCases.Modules;

public class CreateModuleUseCase(AuthDbContext context)
{
    public async Task<Result<ModuleDto>> Execute(CreateModuleDto dto)
    {
        var newModule = new Module
        {
            Name = dto.Name,
            Description = dto.Description,
            Icon = dto.Icon,
            Route = dto.Route,
            Features = dto.Features.Select(f => new Feature()
            {
                Name = f.Name,
                Icon = f.Icon,
                Description =  f.Description,
                Route = f.Route,
                
            }).ToList()
        };
        context.Add(newModule);
        await context.SaveChangesAsync();
        return new ModuleDto
        {
            Id = newModule.Id,
            Name = newModule.Name,
            Description = newModule.Description,
            Icon = newModule.Icon,
            IsEnabled = newModule.IsEnabled,
        };
    }
}