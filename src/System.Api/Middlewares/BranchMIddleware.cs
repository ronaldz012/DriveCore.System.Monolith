using Auth.Contracts.Interfaces;

namespace System.Api.Middlewares;

public class BranchMiddleware(RequestDelegate next, ILogger<BranchMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ICurrentUser currentUser)           
    {
        // Sólo aplica a rutas autenticadas (el JWT ya fue validado antes)
        if (context.User.Identity?.IsAuthenticated == true)
        {
            if (currentUser.BranchIds.Count == 0)
            {
                context.Response.StatusCode = 400;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    StatusCode = 400,
                    Message = "Se requiere al menos una sucursal válida.",
                    Details = "Envíe el header 'X-Branch-Id' con uno o más IDs numéricos separados por coma."
                });
                return;
            }

            var allowedBranches = await currentUser.GetBranchesAsync();
            var allowedIds = allowedBranches.Select(x => x.BranchId).ToHashSet();
            var unauthorized = currentUser.BranchIds.Except(allowedIds).ToList();

            if (unauthorized.Count > 0)
            {
                logger.LogWarning(
                    "Usuario {UserId} intentó acceder a sucursales no permitidas: {Ids}",
                    currentUser.UserId, string.Join(", ", unauthorized));

                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    StatusCode = 403,
                    Message = $"Sin acceso a las sucursales: {string.Join(", ", unauthorized)}.",
                    Details = "Verifique el header 'X-Branch-Id'."
                });
                return;
            }
        }

        await next(context);
    }
}