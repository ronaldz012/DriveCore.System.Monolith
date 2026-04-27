using Auth.Contracts.Dtos.Users;
using Auth.Contracts.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Build.Experimental.ProjectCache;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Shared.Result;
using Shared.Services;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace System.Api.Filters;

/// <summary>
/// Atributo para exigir que la petición contenga al menos una sucursal válida 
/// en el contexto del usuario actual (vía header X-Branch-Id).
/// </summary>
public class RequireBranchAttribute : TypeFilterAttribute
{
    public RequireBranchAttribute() : base(typeof(RequireBranchFilter)) { }
}

public class RequireBranchFilter( ICurrentUser currentUser , ILogger<RequireBranchFilter> logger) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (currentUser.BranchIds.Count == 0)
        {
            context.Result = new ObjectResult(new
            {
                StatusCode = 400,
                Message = "Se requiere al menos una sucursal válida.",
                Details = "Envíe el header 'X-Branch-Id' con un ID numérico válido."
            }) { StatusCode = 400 };
            return;
        }

        var allowedBranches = await currentUser.GetBranchesAsync();
        var allowedIds = allowedBranches.Select(x => x.BranchId).ToHashSet();
        var unauthorized = currentUser.BranchIds.Except(allowedIds).ToList();

        if (unauthorized.Count > 0)
        {
            logger.LogWarning("Usuario {UserId} intentó acceder a sucursales no permitidas: {Ids}",
                currentUser.UserId, string.Join(", ", unauthorized));

            context.Result = new ObjectResult(new
            {
                StatusCode = 403,
                Message = $"Sin acceso a las sucursales: {string.Join(", ", unauthorized)}.",
                Details = "Verifique el header 'X-Branch-Id'."
            }) { StatusCode = 403 };
        }
    }

}


public class BranchHeaderFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Verifica si el atributo [RequireBranch] está presente en el método o en la clase
        var hasRequireBranch = context.MethodInfo.DeclaringType?
            .GetCustomAttributes(true)
            .Union(context.MethodInfo.GetCustomAttributes(true))
            .OfType<RequireBranchAttribute>()
            .Any() ?? false;

        if (hasRequireBranch)
        {
            operation.Parameters ??= new List<OpenApiParameter>();

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Branch-Id",
                In = ParameterLocation.Header,
                Description = "IDs de sucursal separados por coma (ej: 1, 2, 3)",
                Required = true, // Como el filtro lo exige, lo marcamos obligatorio en UI
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Default = new OpenApiString("")
                }
            });
        }
    }
}