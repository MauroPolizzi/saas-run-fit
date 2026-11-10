namespace RunFitPlatform.Api.Shared.Web;

/// <summary>
/// Redes de confianza para <c>X-Forwarded-For</c> y <c>X-Forwarded-Proto</c> (sección <c>ReverseProxy</c>).
/// Vacío por defecto: no se confía en ningún proxy hasta definir el hosting (D6).
/// </summary>
public sealed class ReverseProxyOptions
{
    public const string SectionName = "ReverseProxy";

    /// <summary>Redes en notación CIDR, por ejemplo <c>10.0.0.0/8</c>.</summary>
    public string[] KnownNetworks { get; set; } = [];
}
