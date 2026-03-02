using Auth.Contracts.Dtos.Users;
using Auth.Data.Persistence;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;
using Shared.Result;

namespace Auth.UseCases.Users;

public class GetAllUsers(AuthDbContext context)
{
    public async Task<Result<PagedResultDto<UserDetailsDto>>>  execute(UserQueryDto request)
    {
        var query = context.Users.AsQueryable();
        if (request.Email != null)
        {
            query = query.Where(u => u.Email.Contains(request.Email));
        }
        var (pagedQuery, totalCount) = query.ApplyFilters(request);

        return new PagedResultDto<UserDetailsDto>()
        {
            Items = await pagedQuery.ProjectToType<UserDetailsDto>().ToListAsync(),
            Page = request.GetPageValue(),
            PageSize = request.GetPageSizeValue(),
            TotalCount = totalCount

        };

    }
    
}