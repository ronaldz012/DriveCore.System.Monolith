using Common.Contracts.authentication;
using Common.Contracts.authentication.dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Module.Auth.Application.Abstraction;

namespace System.Api.Filters;

public class RequireFeatureFilter(
    string feature,
    string permission,
    bool multiBranch,
    ISessionStateService sessionState,
    ILogger<RequireFeatureFilter> logger) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var sessionResult = sessionState.GetSessionAsync();
        var userResult = sessionState.GetActorContext();
        if (!sessionResult.IsSuccess || !userResult.IsSuccess)
        {
            context.Result = new ObjectResult(new
            {
                StatusCode = StatusCodes.Status401Unauthorized,
                Message = "No se pudo autenticar la sesión."
            }) { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }

        var session = sessionResult.Value;
        var user = userResult.Value;

        if (session.User.IsAdmin) return;

        if (!multiBranch && user.BranchIds.Count > 1)
        {
            context.Result = new ObjectResult(new
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = "Este endpoint solo acepta una sucursal activa.",
                Details = "Envíe un único ID en el header 'X-Branch-Id'."
            }) { StatusCode = StatusCodes.Status400BadRequest };
            return;
        }

        if (!user.BranchIds.Any())
        {
            context.Result = new ObjectResult(new
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = "No se especificó ninguna sucursal.",
                Details = "Debe enviar el header 'X-Branch-Id' con un ID válido."
            }) { StatusCode = StatusCodes.Status400BadRequest };
            return;
        }

        var requestedBranches = session.Branches
            .Where(b => user.BranchIds.Contains(b.BranchId))
            .ToList();

        if (requestedBranches.Count != user.BranchIds.Count)
        {
            context.Result = new ObjectResult(new
            {
                StatusCode = StatusCodes.Status403Forbidden,
                Message = "Acceso denegado a una o más sucursales solicitadas."
            }) { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        var permissionResult = sessionState.HasPermission(feature, permission);
        if (!permissionResult.IsSuccess)
        {
            context.Result = new ObjectResult(new
            {
                StatusCode = StatusCodes.Status401Unauthorized,
                Message = "No se pudo autenticar la sesión."
            }) { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }

        // Admins and "*" wildcards are resolved inside HasPermission.
        if (!permissionResult.Value)
        {
            logger.LogWarning(
                "Usuario {UserId} fue rechazado. Falta el permiso '{Permission}' en la Feature '{Feature}' para las Branches: {Branches}",
                user.UserId, permission, feature, string.Join(", ", user.BranchIds));

            context.Result = new ObjectResult(new
            {
                StatusCode = StatusCodes.Status403Forbidden,
                Message = $"No tiene el permiso necesario para realizar esta acción.",
                Details = $"Se requiere la acción '{permission}' en la sección '{feature}'."
            }) { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}
