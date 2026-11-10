using RunFitPlatform.Api.Shared.Web;

namespace RunFitPlatform.Api.Modules.Billing;

/// <summary>
/// Módulo Billing: Suscripción del entrenador, catálogo de planes (en código) y derechos.
/// Sin casos de uso todavía (Fase 3: solo estructura).
/// </summary>
public static class BillingModule
{
    public static IServiceCollection AddBillingModule(this IServiceCollection services) => services;

    public static ApiRouteGroups MapBillingEndpoints(this ApiRouteGroups routes) => routes;
}
