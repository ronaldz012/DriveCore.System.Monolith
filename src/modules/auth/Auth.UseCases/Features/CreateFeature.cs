using Auth.Contracts.Dtos.Features;
using Auth.Data.Entities;
using Auth.Data.Persistence;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Shared.Result;

namespace Auth.UseCases.Features;

public class CreateFeature(AuthDbContext dbContext, IMapper mapper)
{

    public async Task<Result<int>> Execute(CreateFeatureDto dto)
    {
        var exists = await dbContext.Features
            .AnyAsync(m => m.Name == dto.Name);
        if (exists) return new Error("DUPLICATE", "Ya existe un módulo con ese nombre");

        var feature = mapper.Map<Feature>(dto);
        dbContext.Features.Add(feature);
        await dbContext.SaveChangesAsync();
        return feature.Id;
    }
}
