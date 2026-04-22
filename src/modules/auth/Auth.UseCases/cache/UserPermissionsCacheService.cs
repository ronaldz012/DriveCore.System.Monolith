using System;
using Auth.Contracts.Dtos.Users;
using Auth.Contracts.Interfaces;
using Auth.Data.Entities;
using Auth.Data.Persistence;
using Auth.UseCases.Autentication;
using Auth.UseCases.Autentication.functions;
using Branches.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Auth.UseCases.cache;

public class UserPermissionsCacheService(IMemoryCache cache, AuthDbContext context, IBranchService branchService) : IUserPermissionsCacheService
{
    private static readonly MemoryCacheEntryOptions Opts =
        new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

    private static string Key(int userId) => $"user_branches:{userId}";

    public async Task<List<BranchAccessDto>> GetAsync(int userId)
    {
        if (cache.TryGetValue(Key(userId), out List<BranchAccessDto>? cached) && cached is not null)
            return cached;
        var user = await context.Users
            .AsSplitQuery()
            .Include(u => u.UserBranchRoles.Where(ur => ur.DeletedAt == null))
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RoleModulePermissions)
                .ThenInclude(rmp => rmp.Module)
                .ThenInclude(m => m.Menus)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return [];

        var branchResult = await UserMappingUtils.BuildBranchAccess(user, branchService);
        if (!branchResult.IsSuccess) return [];

        cache.Set(Key(userId), branchResult.Value, Opts);
        return branchResult.Value;
    }

    public void Invalidate(int userId) =>
        cache.Remove(Key(userId));

    public void Set(int userId, List<BranchAccessDto> branches)
    {
        cache.Set(Key(userId), branches, Opts);
    }
}
