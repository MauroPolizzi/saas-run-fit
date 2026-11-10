using RunFitPlatform.Api.Shared.Web;

namespace RunFitPlatform.Api.Modules.Planning;

/// <summary>
/// Módulo Planning: Catálogo de ejercicios, workouts (running y fuerza), plantillas, sesiones planificadas, versiones y asignaciones.
/// Sin casos de uso todavía (Fase 3: solo estructura).
/// </summary>
public static class PlanningModule
{
    public static IServiceCollection AddPlanningModule(this IServiceCollection services) => services;

    public static ApiRouteGroups MapPlanningEndpoints(this ApiRouteGroups routes) => routes;
}
