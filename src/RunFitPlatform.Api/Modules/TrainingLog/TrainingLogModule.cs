using RunFitPlatform.Api.Shared.Web;

namespace RunFitPlatform.Api.Modules.TrainingLog;

/// <summary>
/// Módulo TrainingLog: Registros realizados, tramos de running, series reales y comentarios.
/// Sin casos de uso todavía (Fase 3: solo estructura).
/// </summary>
public static class TrainingLogModule
{
    public static IServiceCollection AddTrainingLogModule(this IServiceCollection services) => services;

    public static ApiRouteGroups MapTrainingLogEndpoints(this ApiRouteGroups routes) => routes;
}
