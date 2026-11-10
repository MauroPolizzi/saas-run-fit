using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RunFitPlatform.Api.Modules.Accounts.Features.Antiforgery;

/// <summary>
/// <c>GET /api/auth/antiforgery</c>: emite la cookie legible <c>XSRF-TOKEN</c> que Angular devuelve en la
/// cabecera <c>X-XSRF-TOKEN</c>. Angular la pide al iniciar y después de login y logout, porque el token
/// queda ligado a la identidad.
/// </summary>
public static class GetAntiforgeryToken
{
    public const string CookieName = "XSRF-TOKEN";

    public static void Map(RouteGroupBuilder auth) =>
        auth.MapGet("/antiforgery", Handle)
            .WithName(nameof(GetAntiforgeryToken))
            .AllowAnonymous();

    internal static NoContent Handle(HttpContext http, IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(http);

        http.Response.Cookies.Append(CookieName, tokens.RequestToken!, new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
        });

        return TypedResults.NoContent();
    }
}
