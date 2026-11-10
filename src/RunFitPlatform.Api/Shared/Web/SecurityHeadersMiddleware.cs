namespace RunFitPlatform.Api.Shared.Web;

/// <summary>Cabeceras de seguridad de la sección 7.10 (HSTS se agrega con <c>UseHsts</c> fuera de Development).</summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment environment)
{
    public const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; " +
        "connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";

    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers["Referrer-Policy"] = "no-referrer";

        // La referencia visual de OpenAPI (solo Development) necesita scripts propios.
        if (!(environment.IsDevelopment() && IsDevelopmentToolPath(context.Request.Path)))
        {
            headers.ContentSecurityPolicy = ContentSecurityPolicy;
        }

        return next(context);
    }

    private static bool IsDevelopmentToolPath(PathString path) =>
        path.StartsWithSegments("/scalar", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase);
}
