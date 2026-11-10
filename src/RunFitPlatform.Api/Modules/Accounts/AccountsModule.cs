using RunFitPlatform.Api.Modules.Accounts.Features.Antiforgery;
using RunFitPlatform.Api.Shared.Web;

namespace RunFitPlatform.Api.Modules.Accounts;

/// <summary>
/// Módulo Accounts: usuarios, login, logout, contraseñas, 2FA y aceptación de invitaciones.
/// Dueño de las tablas <c>identity.*</c>. En la Fase 3 solo expone el token antiforgery.
/// </summary>
public static class AccountsModule
{
    public static IServiceCollection AddAccountsModule(this IServiceCollection services) => services;

    public static ApiRouteGroups MapAccountsEndpoints(this ApiRouteGroups routes)
    {
        ArgumentNullException.ThrowIfNull(routes);
        GetAntiforgeryToken.Map(routes.Auth);
        return routes;
    }
}
