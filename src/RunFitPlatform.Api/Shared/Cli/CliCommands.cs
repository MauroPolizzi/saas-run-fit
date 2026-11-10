namespace RunFitPlatform.Api.Shared.Cli;

/// <summary>
/// Comandos de mantenimiento que se ejecutan con el mismo binario, sin levantar el servidor web:
/// <c>dotnet RunFitPlatform.Api.dll &lt;comando&gt;</c>. El comando <c>admin create</c> se agrega en el Hito 1.
/// </summary>
public static class CliCommands
{
    /// <summary>Devuelve el código de salida si <paramref name="args"/> es un comando; si no, <c>null</c>.</summary>
    public static async Task<int?> TryRunAsync(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        return args switch
        {
            ["healthcheck", ..] => await HealthCheckAsync(),
            _ => null,
        };
    }

    /// <summary>
    /// <c>HEALTHCHECK</c> de la imagen Docker. La imagen chiseled no tiene shell ni curl,
    /// así que el propio binario consulta <c>/health/live</c>.
    /// </summary>
    private static async Task<int> HealthCheckAsync()
    {
        var ports = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS") ?? "8080";
        var port = ports.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            using var response = await client.GetAsync(new Uri($"http://localhost:{port}/health/live"));
            return response.IsSuccessStatusCode ? 0 : 1;
        }
        catch (HttpRequestException)
        {
            return 1;
        }
        catch (TaskCanceledException)
        {
            return 1;
        }
    }
}
