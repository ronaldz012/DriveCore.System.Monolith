using Auth.Contracts.Dtos.Users;
using Auth.Contracts.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace System.Api.Filters;

public class RequireFeatureFilter(
    string moduleRoute,
    string permission,
    bool multiBranch,         // ← nuevo
    ICurrentUser currentUser,
    ILogger<RequireFeatureFilter> logger) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var branches = await currentUser.GetBranchesAsync();
        var activeBranches = branches
            .Where(b => currentUser.BranchIds.Contains(b.BranchId))
            .ToList();

        if (!multiBranch && currentUser.BranchIds.Count > 1)
        {
            // Single-branch endpoint: no acepta múltiples IDs
            context.Result = new ObjectResult(new
            {
                StatusCode = 400,
                Message = "Este endpoint solo acepta una sucursal.",
                Details = "Envíe un único ID en el header 'X-Branch-Id'."
            }) { StatusCode = 400 };
            return;
        }

        bool hasPermission;

        if (multiBranch)
        {
            // Estadísticas: TODAS las branches activas deben tener el permiso
            hasPermission = activeBranches.All(b =>
                b.Features.Any(m => m.Route == moduleRoute && HasPerm(m, permission)));
        }
        else
        {
            // Single-branch: la única branch activa debe tener el permiso
            hasPermission = activeBranches.All(b =>
                b.Features.Any(m => m.Route == moduleRoute && HasPerm(m, permission)));
            // All() con un solo elemento == same as checking that one branch
        }

        if (!hasPermission)
        {
            logger.LogWarning(
                "Usuario {UserId} sin permiso '{Permission}' en módulo '{Module}'. Branches: {Branches}",
                currentUser.UserId, permission, moduleRoute,
                string.Join(", ", currentUser.BranchIds));

            context.Result = new ObjectResult(new
            {
                StatusCode = 403,
                Message = $"No tiene permiso '{permission}' en el módulo '{moduleRoute}'.",
            }) { StatusCode = 403 };
        }
    }

    private static bool HasPerm(FeaturePermissionsDeductedDto m, string permission) =>
        permission switch
        {
            "read"   => m.CanRead,
            "create" => m.CanCreate,
            "update" => m.CanUpdate,
            "delete" => m.CanDelete,
            _        => false
        };
}