using RunFitPlatform.Api.Shared.Security;

namespace RunFitPlatform.Api.Shared.Web;

/// <summary>
/// Grupos de rutas de la API. Cada módulo registra sus endpoints en el grupo de su perspectiva:
/// <c>/api/coach</c> (entrenador), <c>/api/me</c> (alumno), <c>/api/admin</c> (administración),
/// <c>/api/auth</c> e <c>/api/invitations</c>.
/// </summary>
public sealed record ApiRouteGroups(
    RouteGroupBuilder Api,
    RouteGroupBuilder Auth,
    RouteGroupBuilder Invitations,
    RouteGroupBuilder Coach,
    RouteGroupBuilder Me,
    RouteGroupBuilder Admin)
{
    public static ApiRouteGroups Map(IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api");
        api.AddEndpointFilter<AntiforgeryValidationFilter>();

        return new ApiRouteGroups(
            Api: api,
            Auth: api.MapGroup("/auth").WithTags("Auth"),
            Invitations: api.MapGroup("/invitations").WithTags("Invitations"),
            Coach: api.MapGroup("/coach").WithTags("Coach").RequireAuthorization(Policies.Coach),
            Me: api.MapGroup("/me").WithTags("Me").RequireAuthorization(Policies.Athlete),
            Admin: api.MapGroup("/admin").WithTags("Admin")
                .RequireAuthorization(Policies.PlatformAdmin)
                .RequireRateLimiting(RateLimitPolicies.Admin));
    }
}
