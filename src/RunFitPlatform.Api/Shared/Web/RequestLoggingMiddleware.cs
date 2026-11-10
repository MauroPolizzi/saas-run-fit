using System.Diagnostics;
using System.Security.Claims;

namespace RunFitPlatform.Api.Shared.Web;

/// <summary>
/// Un log por petición: método, plantilla de ruta (no la URL concreta), estado, duración e id de usuario.
/// Nunca registra cuerpos, cabeceras, cookies ni query strings. El <c>traceId</c> llega por el scope de logging.
/// </summary>
public sealed partial class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var start = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        finally
        {
            var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "(sin ruta)";
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "-";
            var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

            if (context.Request.Path.StartsWithSegments("/health"))
            {
                LogHealthRequest(logger, context.Request.Method, route, context.Response.StatusCode, elapsedMs);
            }
            else
            {
                LogRequest(logger, context.Request.Method, route, context.Response.StatusCode, elapsedMs, userId);
            }
        }
    }

    [LoggerMessage(EventId = 1000, Level = LogLevel.Information,
        Message = "HTTP {Method} {Route} respondió {StatusCode} en {ElapsedMs:0.0} ms (usuario {UserId})")]
    private static partial void LogRequest(ILogger logger, string method, string route, int statusCode, double elapsedMs, string userId);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Debug,
        Message = "HTTP {Method} {Route} respondió {StatusCode} en {ElapsedMs:0.0} ms")]
    private static partial void LogHealthRequest(ILogger logger, string method, string route, int statusCode, double elapsedMs);
}
