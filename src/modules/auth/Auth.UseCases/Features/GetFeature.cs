using Auth.Contracts.Dtos.Features;
using Auth.Data.Persistence;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Shared.Result;

namespace Auth.UseCases.Features;

public class GetFeature(AuthDbContext dbContext, IMapper mapper)
{
    public async Task<Result<FeatureDetailsDto?>> Execute(int id)
    {
        var feature = await dbContext.Features.Where(m => m.Id == id).Include(x => x.Module).FirstOrDefaultAsync();
        if (feature == null)
            return new Error("NOT_FOUND", "Module not found");

        return new FeatureDetailsDto()
        {
            Id = feature.Id,
            Name = feature.Name,
            Description = feature.Description,
            Icon = feature.Icon,
            ModuleId = feature.ModuleId,
            ModuleName = feature.Module.Name
        };
    }
}
