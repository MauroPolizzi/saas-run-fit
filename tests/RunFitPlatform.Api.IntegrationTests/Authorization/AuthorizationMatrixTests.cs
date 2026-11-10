using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using RunFitPlatform.Api.IntegrationTests.Infrastructure;
using RunFitPlatform.Api.Shared.Web;

namespace RunFitPlatform.Api.IntegrationTests.Authorization;

/// <summary>
/// Matriz de autorización (sección 11.4). Todo endpoint registrado debe tener su fila; un endpoint nuevo
/// sin fila hace fallar CI. Los endpoints anónimos deben estar marcados como tales en la matriz.
/// </summary>
/// <remarks>
/// Hoy solo se ejecuta el actor anónimo: los demás actores (alumno propio y ajeno, entrenador propio y ajeno,
/// administrador) se crean con los flujos reales de invitación y login, que llegan en el Hito 1.
/// </remarks>
public sealed class AuthorizationMatrixTests(ApiFixture api)
{
    public enum Actor
    {
        Anonymous,
        OwnAthlete,
        OtherAthlete,
        OwnCoach,
        OtherCoach,
        PlatformAdmin,
    }

    /// <summary>Fila de la matriz: endpoint, si es anónimo, una URL de ejemplo y el estado esperado por actor.</summary>
    public sealed record Row(
        string Method,
        string Pattern,
        bool IsAnonymous,
        string SampleUrl,
        IReadOnlyDictionary<Actor, HttpStatusCode> Expected);

    public static readonly Row[] Matrix =
    [
        new("GET", "/api/auth/antiforgery", IsAnonymous: true, "/api/auth/antiforgery",
            new Dictionary<Actor, HttpStatusCode> { [Actor.Anonymous] = HttpStatusCode.NoContent }),
        new("*", "/health/live", IsAnonymous: true, "/health/live",
            new Dictionary<Actor, HttpStatusCode> { [Actor.Anonymous] = HttpStatusCode.OK }),
        new("*", "/health/ready", IsAnonymous: true, "/health/ready",
            new Dictionary<Actor, HttpStatusCode> { [Actor.Anonymous] = HttpStatusCode.OK }),
        // La SPA es pública; la seguridad está en la API. Sin build de Angular en los tests no hay index.html.
        new("GET", WebApplicationExtensions.SpaFallbackPattern, IsAnonymous: true, "/entrenador/alumnos",
            new Dictionary<Actor, HttpStatusCode> { [Actor.Anonymous] = HttpStatusCode.NotFound }),
        new("HEAD", WebApplicationExtensions.SpaFallbackPattern, IsAnonymous: true, "/entrenador/alumnos",
            new Dictionary<Actor, HttpStatusCode> { [Actor.Anonymous] = HttpStatusCode.NotFound }),
    ];

    public static TheoryData<string, string> MatrixRows()
    {
        var data = new TheoryData<string, string>();
        foreach (var row in Matrix)
        {
            data.Add(row.Method, row.Pattern);
        }

        return data;
    }

    [Fact]
    public void EveryEndpoint_HasARowInTheMatrix()
    {
        var missing = RegisteredEndpoints()
            .Where(e => !Matrix.Any(r => r.Method == e.Method && r.Pattern == e.Pattern))
            .Select(e => $"{e.Method} {e.Pattern}")
            .ToList();

        Assert.True(missing.Count == 0,
            "Endpoints sin fila en la matriz de autorización:\n" + string.Join("\n", missing));
    }

    [Fact]
    public void Matrix_HasNoStaleRows()
    {
        var registered = RegisteredEndpoints().ToList();
        var stale = Matrix
            .Where(r => !registered.Any(e => e.Method == r.Method && e.Pattern == r.Pattern))
            .Select(r => $"{r.Method} {r.Pattern}")
            .ToList();

        Assert.True(stale.Count == 0, "Filas de la matriz sin endpoint registrado:\n" + string.Join("\n", stale));
    }

    [Fact]
    public void AnonymousEndpoints_AreDeclaredAsAnonymousInTheMatrix()
    {
        var undeclared = RegisteredEndpoints()
            .Where(e => e.AllowsAnonymous)
            .Where(e => !Matrix.Any(r => r.Method == e.Method && r.Pattern == e.Pattern && r.IsAnonymous))
            .Select(e => $"{e.Method} {e.Pattern}")
            .ToList();

        Assert.True(undeclared.Count == 0, "Endpoints anónimos no permitidos:\n" + string.Join("\n", undeclared));
    }

    [Theory]
    [MemberData(nameof(MatrixRows))]
    public async Task Endpoint_AsAnonymous_ReturnsExpectedStatus(string method, string pattern)
    {
        var row = Matrix.Single(r => r.Method == method && r.Pattern == pattern);
        Assert.True(row.Expected.ContainsKey(Actor.Anonymous), $"Falta el caso anónimo para {method} {pattern}");

        using var client = api.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method == "*" ? "GET" : method), row.SampleUrl);
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(row.Expected[Actor.Anonymous], response.StatusCode);
    }

    private IEnumerable<(string Method, string Pattern, bool AllowsAnonymous)> RegisteredEndpoints() =>
        api.Factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .SelectMany(e =>
            {
                var methods = e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods is { Count: > 0 } m
                    ? m
                    : (IReadOnlyList<string>)["*"];
                var allowsAnonymous = e.Metadata.GetMetadata<IAllowAnonymous>() is not null;
                return methods.Select(method => (method, e.RoutePattern.RawText ?? string.Empty, allowsAnonymous));
            });
}
