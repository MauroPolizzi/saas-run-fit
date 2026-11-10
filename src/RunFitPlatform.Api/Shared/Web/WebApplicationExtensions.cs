using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using RunFitPlatform.Api.Shared.Persistence;
using Scalar.AspNetCore;

namespace RunFitPlatform.Api.Shared.Web;

public static class WebApplicationExtensions
{
    /// <summary>Middleware en el orden requerido por la sección 7.</summary>
    public static WebApplication UseAppPipeline(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseMiddleware<SecurityHeadersMiddleware>();

        // Aplicación Angular compilada (wwwroot en la imagen Docker).
        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.UseRouting();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();

        return app;
    }

    /// <summary>
    /// <c>/health/live</c>: el proceso responde, sin tocar la base. <c>/health/ready</c>: la base responde.
    /// Ambos anónimos y sin información interna.
    /// </summary>
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
            .AllowAnonymous();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains(HealthTags.Ready) })
            .AllowAnonymous();
    }

    /// <summary>Documento OpenAPI y su referencia visual: solo en Development.</summary>
    public static void MapDevelopmentTools(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        app.MapOpenApi().AllowAnonymous();
        app.MapScalarApiReference().AllowAnonymous();
    }

    /// <summary>
    /// Rutas de la SPA: cualquier ruta que no sea de la API, de health checks ni un archivo
    /// devuelve <c>index.html</c>. Las rutas <c>/api</c> desconocidas siguen respondiendo con ProblemDetails.
    /// </summary>
    public static void MapSpaFallback(this WebApplication app) =>
        app.MapFallbackToFile(SpaFallbackPattern, "index.html").AllowAnonymous();

    /// <summary>Patrón de la ruta de la SPA: excluye <c>/api</c>, <c>/health</c> y las herramientas de Development.</summary>
    public const string SpaFallbackPattern = "{*path:nonfile:regex(^(?!api/|api$|health/|openapi/|scalar).*$)}";
}
