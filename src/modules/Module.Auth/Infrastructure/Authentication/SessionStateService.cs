using Common.Contracts.authentication;
using Common.Contracts.authentication.dtos;
using Common.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Module.Auth.Application.Abstraction;
using Module.Auth.Domain;

namespace Module.Auth.Infrastructure.Authentication;

public class SessionStateService(
    IMemoryCache cache,
    IAuthDbContext context,
    IHttpContextAccessor httpContextAccessor) : ISessionStateService
{
    private AuthenticatedSessionDto? _session;

    private static readonly MemoryCacheEntryOptions CacheOpts =
        new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

    private static string Key(string externalAuthId) => $"session_state:{externalAuthId}";

    public async Task<Result<AuthenticatedSessionDto>> AuthenticateByExternalIdAsync(string externalAuthId)
    {
        // 1. Buscar en la caché (compartida entre requests). Si está, hidratar _session y devolver.
        if (cache.TryGetValue(Key(externalAuthId), out AuthenticatedSessionDto? cached) && cached is not null)
        {
            _session = cached;
            return cached;
        }

        // 2. No está cacheado: armar la sesión desde la DB.
        var user = await context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == externalAuthId);

        if (user is null)
            return new Error(ErrorCode.NotFound, "No user is linked to this external account");

        if (!user.IsActive)
            return new Error(ErrorCode.Unauthorized, "User account is inactive");

        var tenant = await context.Tenants
            .IgnoreQueryFilters()
            .Include(t => t.Plan)
            .Include(t => t.TenantDataBase)
            .Include(t => t.Branches)
            .FirstAsync(t => t.Id == user.TenantId);

        var session = await BuildAsync(user, tenant);

        var authenticatedSession = new AuthenticatedSessionDto
        {
            Session = session,
            Schema = tenant.TenantDataBase.Schema,
            DatabaseName = tenant.TenantDataBase.Name,
            ExternalAuthId = externalAuthId
        };

        cache.Set(Key(externalAuthId), authenticatedSession, CacheOpts);

        _session = authenticatedSession;

        return authenticatedSession;
    }

    public Result<SessionStateDto> GetSessionAsync()
    {
        if (_session is null)
            return new Error(ErrorCode.Unauthorized, "Session is not hydrated");

        return _session.Session;
    }

    public Result<ActorContext> GetActorContext()
    {
        if (_session is null)
            return new Error(ErrorCode.Unauthorized, "Session is not hydrated");

        var user = _session.Session.User;

        var branchHeader = httpContextAccessor.HttpContext?.Request.Headers["X-Branch-Id"].ToString();

        var branchIds = string.IsNullOrWhiteSpace(branchHeader)
            ? Array.Empty<Guid>()
            : branchHeader.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Guid.TryParse(s.Trim(), out var id) ? id : Guid.Empty)
                .Where(id => id != Guid.Empty)
                .ToArray();

        var actor = new ActorContext(
            user.TenantId,
            user.Id,
            $"{user.FirstName} {user.LastName}".Trim(),
            branchIds.FirstOrDefault(),
            branchIds);

        return actor;
    }

    public void Invalidate(string externalAuthId)
    {
        cache.Remove(Key(externalAuthId));
        _session = null;
    }

    public void InvalidateTenant(Guid tenantId)
    {
        _session = null;
    }

    private async Task<SessionStateDto> BuildAsync(User user, Tenant tenant)
    {
        var userDetail = new UserDetailResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            IsAdmin = user.IsAdmin,
            UserType = (int)user.Type,
            FirstName = user.FirstName,
            LastName = user.LastName,
            TenantId = user.TenantId,
            IsActive = user.IsActive,
        };

        List<PermissionsByBranchDto> branches;

        if (user.Type is UserType.TenantAdmin or UserType.Owner)
        {
            var planFeatures = await context.Features
                .Where(f => tenant.Plan.AllowedFeatureKeys.Contains(f.Key))
                .ToListAsync();

            branches = tenant.Branches
                .Where(b => b.IsActive)
                .Select(branch => new PermissionsByBranchDto
                {
                    BranchId = branch.Id,
                    BranchName = branch.Name,
                    Role = "Admin",
                    Features = planFeatures
                        .Where(f => branch.AllowedFeatureKeys.Contains(f.Key))
                        .OrderBy(f => f.Module)
                        .Select(f => new SessionFeatureDto
                        {
                            Key = f.Key,
                            DisplayName = f.DisplayName,
                            Route = f.Route,
                            Icon = f.Icon,
                            Module = f.Module.ToString().ToLower(),
                            IsMenu = f.IsMenu,
                            Permissions = ["*"]
                        })
                        .ToList()
                }).ToList();
        }
        else
        {
            var rawPermissions = await context.UserBranchRoles
                .IgnoreQueryFilters()
                .AsSplitQuery()
                .Where(ubr => ubr.UserId == user.Id && ubr.TenantId == user.TenantId)
                .Select(ubr => new
                {
                    ubr.BranchId,
                    BranchName = ubr.Branch.Name,
                    AllowedFeatureKeys = ubr.Branch.AllowedFeatureKeys,
                    RoleName = ubr.Role.Name,
                    Features = ubr.Role.RoleFeaturePermissions.Select(rfp => new
                    {
                        rfp.FeatureKey,
                        rfp.Feature.IsMenu,
                        rfp.Feature.DisplayName,
                        rfp.Feature.Module,
                        rfp.Feature.Route,
                        rfp.Feature.Icon,
                        rfp.Permissions
                    }).ToList()
                })
                .ToListAsync();

            branches = rawPermissions.Select(b => new PermissionsByBranchDto
            {
                BranchId = b.BranchId,
                BranchName = b.BranchName,
                Role = b.RoleName,
                Features = b.Features
                    .Where(f => b.AllowedFeatureKeys.Contains(f.FeatureKey))
                    .OrderBy(f => f.Module)
                    .Select(f => new SessionFeatureDto
                    {
                        Key = f.FeatureKey,
                        DisplayName = f.DisplayName,
                        Route = f.Route,
                        Icon = f.Icon,
                        Module = f.Module.ToString().ToLower(),
                        IsMenu = f.IsMenu,
                        Permissions = f.Permissions
                    })
                    .ToList()
            }).ToList();
        }

        var activeUsers = await context.Users.IgnoreQueryFilters()
            .CountAsync(u => u.TenantId == tenant.Id && u.IsActive);

        var activeBranches = await context.Branches.IgnoreQueryFilters()
            .CountAsync(b => b.TenantId == tenant.Id && b.IsActive);

        var tenantPlan = new TenantPlanUsageDto(
            tenant.Plan.Name,
            tenant.Plan.AllowedFeatureKeys,
            tenant.Plan.MaxUsers,
            activeUsers,
            tenant.Plan.MaxBranches,
            activeBranches
        );

        return new SessionStateDto(userDetail, branches, tenantPlan);
    }
}
