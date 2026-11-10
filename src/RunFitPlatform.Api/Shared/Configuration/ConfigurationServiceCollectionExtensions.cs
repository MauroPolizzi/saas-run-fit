using RunFitPlatform.Api.Shared.Email;
using RunFitPlatform.Api.Shared.Persistence;
using RunFitPlatform.Api.Shared.Security;
using RunFitPlatform.Api.Shared.Web;

namespace RunFitPlatform.Api.Shared.Configuration;

public static class ConfigurationServiceCollectionExtensions
{
    /// <summary>
    /// Enlaza cada sección de configuración a su clase de opciones y la valida al arrancar:
    /// la aplicación no inicia con configuración incompleta.
    /// </summary>
    public static IServiceCollection AddAppOptions(this IServiceCollection services)
    {
        services.AddValidatedOptions<AppOptions>(AppOptions.SectionName);
        services.AddValidatedOptions<ConnectionStringsOptions>(ConnectionStringsOptions.SectionName);
        services.AddValidatedOptions<DatabaseOptions>(DatabaseOptions.SectionName);
        services.AddValidatedOptions<AuthOptions>(AuthOptions.SectionName);
        services.AddValidatedOptions<EmailOptions>(EmailOptions.SectionName);
        services.AddValidatedOptions<RateLimitingOptions>(RateLimitingOptions.SectionName);
        services.AddValidatedOptions<ReverseProxyOptions>(ReverseProxyOptions.SectionName);
        return services;
    }

    /// <summary>
    /// <c>true</c> cuando la aplicación la ejecuta la herramienta que genera <c>openapi/v1.json</c> durante el build.
    /// En ese caso no hay configuración real y no se valida al arrancar.
    /// </summary>
    public static bool IsBuildTimeDocumentGeneration { get; } =
        System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

    private static void AddValidatedOptions<TOptions>(this IServiceCollection services, string sectionName)
        where TOptions : class
    {
        var builder = services.AddOptions<TOptions>()
            .BindConfiguration(sectionName)
            .ValidateDataAnnotations();

        if (!IsBuildTimeDocumentGeneration)
        {
            builder.ValidateOnStart();
        }
    }
}
