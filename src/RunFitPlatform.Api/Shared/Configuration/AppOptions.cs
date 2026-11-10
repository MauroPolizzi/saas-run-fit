using System.ComponentModel.DataAnnotations;

namespace RunFitPlatform.Api.Shared.Configuration;

/// <summary>Configuración general de la aplicación (sección <c>App</c>).</summary>
public sealed class AppOptions
{
    public const string SectionName = "App";

    /// <summary>URL pública desde la que se arman los enlaces de los emails.</summary>
    [Required, Url]
    public string PublicBaseUrl { get; set; } = string.Empty;

    /// <summary>Zona horaria de referencia del producto.</summary>
    [Required]
    public string TimeZone { get; set; } = "America/Argentina/Buenos_Aires";
}
