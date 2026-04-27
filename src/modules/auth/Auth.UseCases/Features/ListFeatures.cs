using Auth.Contracts.Dtos.Features;
using Auth.Data.Entities;
using Auth.Data.Persistence;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;
using Shared.Result;

namespace Auth.UseCases.Features;

public class ListFeatures(AuthDbContext dbContext)
{
    public async Task<Result<PagedResultDto<FeatureDto>>> Execute(FeatureQueryDto queryDto)
    {
        var query = dbContext.Features.AsQueryable();
        if (!string.IsNullOrEmpty(queryDto.Filter))
            query = query.Where(m => m.Name.Contains(queryDto.Filter ?? string.Empty));

        (query, var totalCount) = query.ApplyFilters(queryDto);
        var result = await query.Select(x => new FeatureDto
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            Icon = x.Icon,
            ModuleId = x.ModuleId,
            ModuleName = x.Module.Name,
        }).ToListAsync();
        return new PagedResultDto<FeatureDto>
        {
            Items = result,
            Page = queryDto.GetPageValue(),
            PageSize = queryDto.GetPageSizeValue(),
            TotalCount = totalCount
        };
                    
    }
}
