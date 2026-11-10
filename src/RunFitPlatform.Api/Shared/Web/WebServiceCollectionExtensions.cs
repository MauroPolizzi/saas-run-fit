using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace RunFitPlatform.Api.Shared.Web;

/// <summary>Errores, JSON, validación, OpenAPI, rate limiting y proxy inverso.</summary>
public static class WebServiceCollectionExtensions
{
    /// <summary>Tamaño máximo del cuerpo de una petición (256 KB).</summary>
    public const long MaxRequestBodySize = 256 * 1024;

    public static IServiceCollection AddAppWeb(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = Customize);
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
        });

        services.AddValidation();

        services.AddOpenApi("v1", options => options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Title = "Run Fit Platform API";
            document.Info.Version = "v1";
            return Task.CompletedTask;
        }));

        AddRateLimiting(services);
        AddForwardedHeaders(services);

        return services;
    }

    /// <summary>Toda respuesta de error lleva <c>traceId</c> y <c>code</c>.</summary>
    private static void Customize(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

        if (!problem.Extensions.ContainsKey(ErrorCodes.ExtensionName))
        {
            var status = problem.Status ?? context.HttpContext.Response.StatusCode;
            problem.Extensions[ErrorCodes.ExtensionName] = ErrorCodes.FromStatusCode(status);
        }
    }

    private static void AddRateLimiting(IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                var problemDetails = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problemDetails.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = new ProblemDetails { Status = StatusCodes.Status429TooManyRequests },
                });
            };

            options.AddPolicy(RateLimitPolicies.Login, http => ByClientIp(http, o => o.Login));
            options.AddPolicy(RateLimitPolicies.PasswordRecovery, http => ByClientIp(http, o => o.PasswordRecovery));
            options.AddPolicy(RateLimitPolicies.InvitationAcceptance, http => ByClientIp(http, o => o.InvitationAcceptance));
            options.AddPolicy(RateLimitPolicies.Admin, http => ByUser(http, o => o.Admin));
        });

    private static RateLimitPartition<string> ByClientIp(HttpContext http, Func<RateLimitingOptions, RateLimitRule> select) =>
        FixedWindow(http, select, http.Connection.RemoteIpAddress?.ToString() ?? "unknown");

    private static RateLimitPartition<string> ByUser(HttpContext http, Func<RateLimitingOptions, RateLimitRule> select) =>
        FixedWindow(http, select, http.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? http.Connection.RemoteIpAddress?.ToString()
            ?? "unknown");

    private static RateLimitPartition<string> FixedWindow(
        HttpContext http, Func<RateLimitingOptions, RateLimitRule> select, string partitionKey)
    {
        var rule = select(http.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value);
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = rule.PermitLimit,
            Window = TimeSpan.FromSeconds(rule.WindowSeconds),
            QueueLimit = 0,
        });
    }

    /// <summary>Solo se confía en las redes del proxy configuradas (D6); sin configuración, en ninguna.</summary>
    private static void AddForwardedHeaders(IServiceCollection services) =>
        services.AddOptions<ForwardedHeadersOptions>()
            .Configure<IOptions<ReverseProxyOptions>>((options, proxy) =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
                foreach (var network in proxy.Value.KnownNetworks)
                {
                    options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
                }
            });
}
