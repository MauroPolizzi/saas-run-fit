using RunFitPlatform.Api.Shared.Web;

namespace RunFitPlatform.Api.Modules.Coaching;

/// <summary>
/// Módulo Coaching: Perfiles de entrenador y alumno, relación entrenador-alumno, grupos, invitaciones de alumnos, salud y consentimientos.
/// Sin casos de uso todavía (Fase 3: solo estructura).
/// </summary>
public static class CoachingModule
{
    public static IServiceCollection AddCoachingModule(this IServiceCollection services) => services;

    public static ApiRouteGroups MapCoachingEndpoints(this ApiRouteGroups routes) => routes;
}
