using System.ComponentModel.DataAnnotations;

namespace RunFitPlatform.Api.Shared.Persistence;

/// <summary>Configuración de la base de datos (sección <c>Database</c>).</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Aplica las migraciones al arrancar. Solo se respeta en Development.</summary>
    public bool MigrateOnStartup { get; set; }

    [Range(1, 600)]
    public int CommandTimeoutSeconds { get; set; } = 30;
}
