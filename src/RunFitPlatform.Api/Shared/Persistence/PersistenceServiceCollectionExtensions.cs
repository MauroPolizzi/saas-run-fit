using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using RunFitPlatform.Api.Shared.Configuration;

namespace RunFitPlatform.Api.Shared.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public const string MigrationsHistoryTable = "__ef_migrations_history";

    public static IServiceCollection AddAppPersistence(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var connectionString = sp.GetRequiredService<IOptions<ConnectionStringsOptions>>().Value.Default;
            var database = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            ConfigureDbContext(options, connectionString, database.CommandTimeoutSeconds);

            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging().EnableDetailedErrors();
            }
        });

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("database", tags: [HealthTags.Ready]);

        return services;
    }

    /// <summary>Configuración del proveedor compartida por la aplicación y los tests.</summary>
    public static DbContextOptionsBuilder ConfigureDbContext(
        DbContextOptionsBuilder options, string connectionString, int commandTimeoutSeconds = 30) =>
        options
            .UseNpgsql(WithoutGssEncryption(connectionString), npgsql => npgsql
                .MigrationsHistoryTable(MigrationsHistoryTable, "public")
                .CommandTimeout(commandTimeoutSeconds))
            .UseSnakeCaseNamingConvention();

    /// <summary>
    /// No se usa Kerberos: sin esto Npgsql intenta cargar <c>libgssapi_krb5</c>, que la imagen chiseled no trae.
    /// Respeta un valor explícito en la cadena de conexión.
    /// </summary>
    internal static string WithoutGssEncryption(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (!connectionString.Contains("GSS Encryption Mode", StringComparison.OrdinalIgnoreCase))
        {
            builder.GssEncryptionMode = GssEncryptionMode.Disable;
        }

        return builder.ConnectionString;
    }

    /// <summary>
    /// Aplica las migraciones pendientes. Solo para Development (<c>Database:MigrateOnStartup</c>):
    /// en producción las migraciones se ejecutan como paso explícito del pipeline.
    /// </summary>
    public static async Task MigrateDatabaseIfConfiguredAsync(this WebApplication app)
    {
        var database = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        if (!database.MigrateOnStartup || !app.Environment.IsDevelopment())
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
}

public static class HealthTags
{
    public const string Ready = "ready";
}
