using RunFitPlatform.Api.Modules.Accounts;
using RunFitPlatform.Api.Modules.Admin;
using RunFitPlatform.Api.Modules.Billing;
using RunFitPlatform.Api.Modules.Coaching;
using RunFitPlatform.Api.Modules.Planning;
using RunFitPlatform.Api.Modules.Progress;
using RunFitPlatform.Api.Modules.TrainingLog;
using RunFitPlatform.Api.Shared.Cli;
using RunFitPlatform.Api.Shared.Configuration;
using RunFitPlatform.Api.Shared.Email;
using RunFitPlatform.Api.Shared.Persistence;
using RunFitPlatform.Api.Shared.Security;
using RunFitPlatform.Api.Shared.Web;

// Comandos de mantenimiento (por ejemplo, el HEALTHCHECK de la imagen Docker).
if (await CliCommands.TryRunAsync(args) is { } exitCode)
{
    return exitCode;
}

var builder = WebApplication.CreateBuilder(args);

// Logging: traceId en cada log; JSON a consola fuera de Development.
builder.Logging.Configure(options =>
    options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId);
if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole(options =>
    {
        options.IncludeScopes = true;
        options.UseUtcTimestamp = true;
        options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
    });
}

builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = WebServiceCollectionExtensions.MaxRequestBodySize;
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services
    .AddAppOptions()
    .AddAppPersistence(builder.Environment)
    .AddAppSecurity()
    .AddAppWeb()
    .AddAppEmail();

builder.Services
    .AddAccountsModule()
    .AddCoachingModule()
    .AddPlanningModule()
    .AddTrainingLogModule()
    .AddProgressModule()
    .AddBillingModule()
    .AddAdminModule();

var app = builder.Build();

await app.MigrateDatabaseIfConfiguredAsync();

app.UseAppPipeline();

ApiRouteGroups.Map(app)
    .MapAccountsEndpoints()
    .MapCoachingEndpoints()
    .MapPlanningEndpoints()
    .MapTrainingLogEndpoints()
    .MapProgressEndpoints()
    .MapBillingEndpoints()
    .MapAdminEndpoints();

app.MapHealthEndpoints();
app.MapDevelopmentTools();
app.MapSpaFallback();

await app.RunAsync();
return 0;

/// <summary>Punto de entrada; parcial y público para <c>WebApplicationFactory</c> en los tests.</summary>
public partial class Program;
