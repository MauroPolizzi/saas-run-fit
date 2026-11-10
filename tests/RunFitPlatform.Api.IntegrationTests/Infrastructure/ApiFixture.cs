using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunFitPlatform.Api.IntegrationTests.Infrastructure;
using RunFitPlatform.Api.Shared.Email;
using RunFitPlatform.Api.Shared.Persistence;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(ApiFixture))]

namespace RunFitPlatform.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Un contenedor PostgreSQL 18 por ejecución de tests, con todas las migraciones aplicadas desde cero
/// (nunca <c>EnsureCreated</c>), y la API real sobre <see cref="WebApplicationFactory{TEntryPoint}"/>.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    public const string PostgresImage = "postgres:18.6";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder(PostgresImage)
        .WithDatabase("runfit_tests")
        .WithUsername("runfit")
        .WithPassword("runfit_tests")
        .Build();

    private ApiFactory? _factory;

    public ApiFactory Factory => _factory ?? throw new InvalidOperationException("La fixture no se inicializó.");

    public InMemoryEmailSender Emails => Factory.Services.GetRequiredService<InMemoryEmailSender>();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new ApiFactory(_postgres.GetConnectionString());

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    /// <summary>Cliente HTTPS con cookies: las cookies <c>__Host-</c> exigen <c>Secure</c>.</summary>
    public HttpClient CreateClient() => Factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }
}

public sealed class ApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", connectionString);
        builder.UseSetting("App:PublicBaseUrl", "https://localhost");
        builder.UseSetting("Database:MigrateOnStartup", "false");
        builder.UseSetting("Email:Provider", nameof(EmailProvider.InMemory));
        builder.UseSetting("Email:From", "no-reply@runfit.test");
    }
}
