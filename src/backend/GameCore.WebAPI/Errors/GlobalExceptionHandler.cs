using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GameCore.WebAPI.Errors;

/// <summary>
/// Last line of defence: converts unhandled exceptions into ProblemDetails without
/// exposing stack traces or internal messages. Requests are validated before they
/// reach the database, so the SQL mappings below only cover race conditions
/// (for example two users saving the same email at the same time).
/// </summary>
public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = Classify(exception);

        if (status >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogWarning("Request {Method} {Path} rejected with {Status}: {Reason}", httpContext.Request.Method, httpContext.Request.Path, status, exception.GetType().Name);

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail }
        });
    }

    internal static (int Status, string Title, string? Detail) Classify(Exception exception) => exception switch
    {
        BadHttpRequestException bad => (bad.StatusCode, "La solicitud no es válida.", "El cuerpo de la solicitud no tiene el formato esperado."),
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflicto con datos existentes.", "El registro fue modificado o eliminado por otra operación."),
        DbUpdateException { InnerException: SqlException sql } when sql.Number is 2601 or 2627 =>
            (StatusCodes.Status409Conflict, "Conflicto con datos existentes.", "Ya existe un registro con esos datos únicos."),
        DbUpdateException { InnerException: SqlException sql } when sql.Number is 547 =>
            (StatusCodes.Status400BadRequest, "La solicitud contiene datos inválidos.", "Uno de los registros relacionados no existe o no cumple las reglas de la base de datos."),
        _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor.", "Ocurrió un error inesperado. Inténtalo de nuevo más tarde.")
    };
}
