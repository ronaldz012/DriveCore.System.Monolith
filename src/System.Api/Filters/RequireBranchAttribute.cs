using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
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

public class RequireBranchFilter(ICurrentUser currentUser) : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Si la lista está vacía, cortamos la ejecución y devolvemos 400 Bad Request
        if (currentUser.BranchIds.Count == 0)
        {
            context.Result = new BadRequestObjectResult(new
            {
                StatusCode = 400,
                Message = "Acceso denegado: Se requiere al menos una sucursal válida.",
                Details = "Asegúrese de enviar el header 'X-Branch-Id' con un ID numérico válido."
            });
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