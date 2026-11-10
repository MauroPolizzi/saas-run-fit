using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunFitPlatform.Api.IntegrationTests.Infrastructure;
using RunFitPlatform.Api.Shared.Persistence;
using RunFitPlatform.Api.Shared.Web;

namespace RunFitPlatform.Api.IntegrationTests.Platform;

public sealed class HealthTests(ApiFixture api)
{
    [Fact]
    public async Task Ready_WithDatabaseAvailable_ReturnsHealthy()
    {
        using var client = api.CreateClient();

        using var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Live_ReturnsHealthyWithoutInternalDetails()
    {
        using var client = api.CreateClient();

        using var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}

public sealed class DatabaseTests(ApiFixture api)
{
    [Fact]
    public async Task Migrations_AreAllAppliedAndMatchTheModel()
    {
        await using var scope = api.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken));
        Assert.False(db.Database.HasPendingModelChanges(), "El modelo cambió y falta una migración.");
    }

    [Fact]
    public async Task Roles_AreSeededByTheInitialMigration()
    {
        await using var scope = api.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var roles = await db.Roles.Select(r => r.Name).OrderBy(n => n).ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["Athlete", "Coach", "PlatformAdmin"], roles);
    }
}

public sealed class HttpPipelineTests(ApiFixture api)
{
    [Fact]
    public async Task UnknownApiRoute_AsAnonymous_Returns401ProblemDetailsWithCode()
    {
        using var client = api.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/coach/no-existe", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(ErrorCodes.Unauthenticated, problem.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrEmpty(problem.RootElement.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Responses_IncludeSecurityHeaders()
    {
        using var client = api.CreateClient();

        using var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(SecurityHeadersMiddleware.ContentSecurityPolicy, response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
    }

    [Fact]
    public async Task Antiforgery_IssuesReadableXsrfCookieAndHttpOnlyCookieToken()
    {
        using var client = api.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/auth/antiforgery", UriKind.Relative), TestContext.Current.CancellationToken);

        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        var xsrf = Assert.Single(cookies, c => c.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal));
        var cookieToken = Assert.Single(cookies, c => c.StartsWith("__Host-cp.af=", StringComparison.Ordinal));

        Assert.Contains("secure", xsrf, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", xsrf, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("httponly", xsrf, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookieToken, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookieToken, StringComparison.OrdinalIgnoreCase);
    }
}
