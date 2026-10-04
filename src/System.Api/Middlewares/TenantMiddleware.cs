using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Module.Auth.Application.Abstraction;
using System.Security.Claims;

namespace System.Api.Middlewares;

public class TenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        ITenantConnectionContext tenantConnectionContext,
        ISessionStateService sessionState)
    {
        var endpoint = context.GetEndpoint();

        if (endpoint is null || endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await next(context);
            return;
        }

        if (endpoint.Metadata.GetMetadata<System.Api.Filters.ApiKeyAttribute>() is not null)
        {
            await next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        // El JWT de Auth0 identifica al usuario por su "sub" (= ExternalAuthId, ej. auth0|...)
        var externalAuthId =  context.User.FindFirst("email")?.Value;

        if (string.IsNullOrEmpty(externalAuthId))
        {
            await WriteProblemDetails(context, StatusCodes.Status401Unauthorized,
                ErrorCode.Unauthorized, "Invalid token: missing subject claim");
            return;
        }

        var result = await sessionState.AuthenticateByExternalIdAsync(externalAuthId);

        if (!result.IsSuccess)
        {
            await WriteProblemDetails(context, ResultStatus(result.Error.Code),
                result.Error.Code, result.Error.Message);
            return;
        }

        var data = result.Value;

        tenantConnectionContext.TenantId = data.Session.User.TenantId;
        tenantConnectionContext.Schema = data.Schema;
        tenantConnectionContext.DatabaseName = data.DatabaseName;

        await next(context);
    }

    private static int ResultStatus(ErrorCode code) => code switch
    {
        ErrorCode.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorCode.Forbidden => StatusCodes.Status403Forbidden,
        ErrorCode.NotFound => StatusCodes.Status404NotFound,
        _ => StatusCodes.Status400BadRequest,
    };

    private static async Task WriteProblemDetails(HttpContext context, int statusCode, ErrorCode errorCode, string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = errorCode.ToString(),
            Detail = detail
        });
    }
}
