using System.ComponentModel.DataAnnotations;

namespace RunFitPlatform.Api.Shared.Configuration;

/// <summary>Cadenas de conexión (sección <c>ConnectionStrings</c>).</summary>
public sealed class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required]
    public string Default { get; set; } = string.Empty;
}
