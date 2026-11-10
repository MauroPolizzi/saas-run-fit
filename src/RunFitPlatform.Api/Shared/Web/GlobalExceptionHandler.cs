using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace RunFitPlatform.Api.Shared.Web;

/// <summary>
/// Traduce las excepciones no esperadas a <c>ProblemDetails</c>. Los resultados esperados (404, 422, etc.)
/// se devuelven con <c>TypedResults</c> desde los handlers, no con excepciones.
/// </summary>
public sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, code) = Classify(exception);

        if (status >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandled(logger, exception, httpContext.Request.Method);
        }
        else if (logger.IsEnabled(LogLevel.Information))
        {
            LogHandled(logger, exception.GetType().Name, status);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Extensions = { [ErrorCodes.ExtensionName] = code },
            },
        });
    }

    internal static (int Status, string Code) Classify(Exception exception) => exception switch
    {
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, ErrorCodes.ConcurrencyConflict),
        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
            (StatusCodes.Status409Conflict, ErrorCodes.UniqueConflict),
        BadHttpRequestException badRequest =>
            (badRequest.StatusCode, ErrorCodes.FromStatusCode(badRequest.StatusCode)),
        _ => (StatusCodes.Status500InternalServerError, ErrorCodes.ServerError),
    };

    [LoggerMessage(EventId = 5000, Level = LogLevel.Error, Message = "Excepción no controlada en una petición {Method}")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string method);

    [LoggerMessage(EventId = 5001, Level = LogLevel.Information, Message = "Excepción {ExceptionType} traducida a {StatusCode}")]
    private static partial void LogHandled(ILogger logger, string exceptionType, int statusCode);
}
