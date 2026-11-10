using RunFitPlatform.Api.Shared.Web;

namespace RunFitPlatform.Api.Modules.Admin;

/// <summary>
/// Módulo Admin: Panel mínimo: invitaciones de entrenador, suscripciones, cuentas, bajas y auditoría. Sin acceso a entrenamientos ni salud (ADR-009).
/// Sin casos de uso todavía (Fase 3: solo estructura).
/// </summary>
public static class AdminModule
{
    public static IServiceCollection AddAdminModule(this IServiceCollection services) => services;

    public static ApiRouteGroups MapAdminEndpoints(this ApiRouteGroups routes) => routes;
}
