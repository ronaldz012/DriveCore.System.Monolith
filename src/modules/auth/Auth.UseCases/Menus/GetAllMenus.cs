using System;
using Auth.Contracts.Dtos.Modules;
using Auth.Data.Persistence;
using Auth.UseCases.mapper;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;
using Shared.Result;
namespace Auth.UseCases.Menus;

public class GetAllMenus(AuthDbContext dbContext)
{
    public async Task<Result<PagedResultDto<MenuDto>>> Execute(MenuQueryDto query)
    {
        var menusQuery = dbContext.Menus.AsQueryable();

        if (query.ParentMenuId.HasValue)
        {
            menusQuery = menusQuery.Where(m => m.ParentMenuId == query.ParentMenuId.Value);
        }

        if (query.ModuleId.HasValue)
        {
            menusQuery = menusQuery.Where(m => m.ModuleId == query.ModuleId.Value);
        }

        (var pagedQuery, var totalCount) = menusQuery.ApplyFilters(query);
        
        return new PagedResultDto<MenuDto>
        {
            Items = await pagedQuery.ProjectToType<MenuDto>().ToListAsync(),
            TotalCount = totalCount,
            Page = query.GetPageValue(),
            PageSize = query.GetPageSizeValue()
        };

    }

}
