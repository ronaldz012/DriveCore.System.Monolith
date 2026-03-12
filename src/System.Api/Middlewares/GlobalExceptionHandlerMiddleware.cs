namespace System.Api.Middlewares;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Middleware que captura todas las excepciones no manejadas y las convierte en ProblemDetails.
/// Centraliza TODO el manejo de errores para toda la API.
/// </summary>
public class GlobalExceptionHandlerMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlerMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error no manejado en {Path}. Usuario: {User}",
                context.Request.Path,
                context.User?.Identity?.Name ?? "Anónimo");

            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Convierte cualquier excepción en una respuesta HTTP con ProblemDetails.
    /// </summary>
    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        // Determinar el código de error y mensaje según el tipo de excepción
        var (errorCode, errorMessage) = MapExceptionToError(ex);
        var statusCode = MapErrorCodeToStatusCode(errorCode);

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = errorCode,
            Detail = errorMessage,
            Instance = context.Request.Path
        };

        // Agregar información adicional en desarrollo
        var environment = context.RequestServices.GetService<IHostEnvironment>();
        if (environment?.IsDevelopment() == true)
        {
            problemDetails.Extensions["exception"] = ex.GetType().Name;
            problemDetails.Extensions["stackTrace"] = ex.StackTrace;
            problemDetails.Extensions["innerException"] = ex.InnerException?.Message;
        }

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    /// <summary>
    /// Mapea una excepción a un código de error y mensaje apropiados.
    /// </summary>
    private static (string Code, string Message) MapExceptionToError(Exception ex)
    {
        return ex switch
        {
            // Excepciones de Entity Framework / Base de datos
            DbUpdateException dbEx => MapDbUpdateException(dbEx),

            // Excepciones de operación inválida
            InvalidOperationException =>
                ("INVALID_OPERATION", GetInnerMostMessage(ex)),

            // Excepciones de autorización
            UnauthorizedAccessException =>
                ("UNAUTHORIZED", "No tiene permisos para realizar esta operación"),

            // Excepciones de argumentos
            ArgumentNullException argEx =>
                ("VALIDATION_ERROR", $"El parámetro '{argEx.ParamName}' es requerido"),

            ArgumentException argEx =>
                ("VALIDATION_ERROR", argEx.Message),

            // Excepciones de no encontrado
            KeyNotFoundException =>
                ("NOT_FOUND", ex.Message),

            // Excepciones de timeout
            TimeoutException =>
                ("TIMEOUT", "La operación tardó demasiado tiempo"),

            // Operación cancelada
            OperationCanceledException =>
                ("CANCELLED", "La operación fue cancelada"),

            // Error genérico
            _ => ("UNEXPECTED_ERROR", $"Error inesperado: {GetInnerMostMessage(ex)}")
        };
    }

    /// <summary>
    /// Mapea excepciones específicas de base de datos a códigos de error apropiados.
    /// </summary>
    private static (string Code, string Message) MapDbUpdateException(DbUpdateException ex)
    {
        var innerMessage = GetInnerMostMessage(ex);

        // Detectar violación de Foreign Key
        if (ContainsAny(innerMessage, "FOREIGN KEY", "FK_"))
        {
            return ("INVALID_REFERENCE",
                "La referencia especificada no existe o es inválida");
        }

        // Detectar violación de Unique Constraint
        if (ContainsAny(innerMessage, "UNIQUE", "duplicate", "IX_", "UQ_"))
        {
            return ("DUPLICATE",
                "Ya existe un registro con estos datos únicos");
        }

        // Detectar violación de Check Constraint
        if (ContainsAny(innerMessage, "CHECK", "CK_"))
        {
            return ("INVALID_DATA",
                "Los datos no cumplen con las reglas de validación");
        }

        // Detectar violación de Primary Key
        if (ContainsAny(innerMessage, "PRIMARY KEY", "PK_"))
        {
            return ("DUPLICATE",
                "Ya existe un registro con ese identificador");
        }

        // Detectar timeout de base de datos
        if (ContainsAny(innerMessage, "timeout"))
        {
            return ("DATABASE_TIMEOUT",
                "La operación en la base de datos tardó demasiado");
        }

        // Detectar problemas de conexión
        if (ContainsAny(innerMessage, "connection", "network"))
        {
            return ("DATABASE_CONNECTION",
                "No se pudo conectar a la base de datos");
        }

        // Error genérico de base de datos
        return ("DATABASE_ERROR",
            $"Error al guardar en la base de datos: {innerMessage}");
    }

    /// <summary>
    /// Obtiene el mensaje de la excepción más interna (donde está el error real).
    /// </summary>
    private static string GetInnerMostMessage(Exception ex)
    {
        var innerException = ex;

        // Navegar hasta la excepción más interna
        while (innerException.InnerException != null)
        {
            innerException = innerException.InnerException;
        }

        return innerException.Message;
    }

    /// <summary>
    /// Verifica si el mensaje contiene alguna de las palabras clave (case-insensitive).
    /// </summary>
    private static bool ContainsAny(string message, params string[] keywords)
    {
        return keywords.Any(keyword =>
            message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Mapea códigos de error personalizados a códigos de estado HTTP.
    /// </summary>
    private static int MapErrorCodeToStatusCode(string errorCode)
    {
        return errorCode switch
        {
            // Validación (400)
            "VALIDATION_ERROR" => StatusCodes.Status400BadRequest,
            "INVALID_OPERATION" => StatusCodes.Status400BadRequest,
            "INVALID_DATA" => StatusCodes.Status400BadRequest,
            "INVALID_REFERENCE" => StatusCodes.Status400BadRequest,

            // Autenticación (401)
            "UNAUTHORIZED" => StatusCodes.Status401Unauthorized,

            // Autorización (403)
            "FORBIDDEN" => StatusCodes.Status403Forbidden,

            // No encontrado (404)
            "NOT_FOUND" => StatusCodes.Status404NotFound,

            // Timeout cliente (408)
            "TIMEOUT" => StatusCodes.Status408RequestTimeout,

            // Conflicto/Duplicado (409)
            "DUPLICATE" => StatusCodes.Status409Conflict,

            // Cancelado por cliente (499 - no estándar pero común)
            "CANCELLED" => 499,

            // Error interno servidor (500)
            "DATABASE_ERROR" => StatusCodes.Status500InternalServerError,
            "UNEXPECTED_ERROR" => StatusCodes.Status500InternalServerError,

            // Servicio no disponible (503)
            "DATABASE_CONNECTION" => StatusCodes.Status503ServiceUnavailable,

            // Gateway timeout (504)
            "DATABASE_TIMEOUT" => StatusCodes.Status504GatewayTimeout,

            // Default: Error interno (500)
            _ => StatusCodes.Status500InternalServerError
        };
    }
}