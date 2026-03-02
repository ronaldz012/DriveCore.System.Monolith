using System;
using System.Reflection;
using Auth.Contracts.Dtos.Modules;
using Auth.Data.Persistence;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;
using Shared.Result;

namespace Auth.UseCases.Modules;

public class GetAllModules(AuthDbContext dbContext)
{
    public async Task<Result<PagedResultDto<ModuleDto>>> Execute(ModuleQueryDto queryDto)
    {
        var query = dbContext.Modules.AsQueryable();
        if (!string.IsNullOrEmpty(queryDto.Filter))
            query = query.Where(m => m.Name.Contains(queryDto.Filter ?? string.Empty));

        (query, var totalCount) = query.ApplyFilters(queryDto);

        return new PagedResultDto<ModuleDto>
        {
            Items = await query.ProjectToType<ModuleDto>().ToListAsync(),
            Page = queryDto.GetPageValue(),
            PageSize = queryDto.GetPageSizeValue(),
            TotalCount = totalCount
        };
                    
    }
}
