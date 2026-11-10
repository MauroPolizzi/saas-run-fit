using RunFitPlatform.Api.Shared.Web;

namespace RunFitPlatform.Api.Modules.Progress;

/// <summary>
/// Módulo Progress: Consultas agregadas de historial y métricas (solo lectura).
/// Sin casos de uso todavía (Fase 3: solo estructura).
/// </summary>
public static class ProgressModule
{
    public static IServiceCollection AddProgressModule(this IServiceCollection services) => services;

    public static ApiRouteGroups MapProgressEndpoints(this ApiRouteGroups routes) => routes;
}
