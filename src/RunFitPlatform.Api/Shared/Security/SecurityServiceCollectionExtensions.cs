using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using RunFitPlatform.Api.Shared.Configuration;
using RunFitPlatform.Api.Shared.Persistence;

namespace RunFitPlatform.Api.Shared.Security;

/// <summary>Identity, cookies, autorización, antiforgery y Data Protection (ADR-005).</summary>
public static class SecurityServiceCollectionExtensions
{
    public const string SessionCookieName = "__Host-cp.session";
    public const string AntiforgeryCookieName = "__Host-cp.af";
    public const string AntiforgeryHeaderName = "X-XSRF-TOKEN";
    public const string DataProtectionApplicationName = "run-fit-platform";

    public static IServiceCollection AddAppSecurity(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IDataScope, HttpDataScope>();

        AddIdentity(services);
        AddCookies(services);
        AddAuthorizationPolicies(services);

        services.AddAntiforgery(options =>
        {
            options.HeaderName = AntiforgeryHeaderName;
            options.Cookie.Name = AntiforgeryCookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

        // Claves en la base: un redeploy no invalida sesiones ni tokens.
        // Durante la generación de OpenAPI en build no hay base, así que no se persisten.
        var dataProtection = services.AddDataProtection().SetApplicationName(DataProtectionApplicationName);
        if (!ConfigurationServiceCollectionExtensions.IsBuildTimeDocumentGeneration)
        {
            dataProtection.PersistKeysToDbContext<AppDbContext>();
        }

        return services;
    }

    private static void AddIdentity(IServiceCollection services)
    {
        services.AddIdentityCore<AppUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddPasswordValidator<MaxLengthPasswordValidator>();

        services.AddOptions<IdentityOptions>()
            .Configure<IOptions<AuthOptions>>((options, auth) =>
            {
                // Mínimo 10 caracteres, sin reglas de composición (sección 7.10).
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredUniqueChars = 1;

                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = auth.Value.MaxFailedAccessAttempts;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(auth.Value.LockoutMinutes);
            });

        services.AddOptions<SecurityStampValidatorOptions>()
            .Configure<IOptions<AuthOptions>>((options, auth) =>
                options.ValidationInterval = TimeSpan.FromMinutes(auth.Value.SecurityStampValidationMinutes));
    }

    private static void AddCookies(IServiceCollection services)
    {
        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        services.AddOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
            .Configure<IOptions<AuthOptions>>((options, auth) =>
            {
                options.Cookie.Name = SessionCookieName;
                options.ExpireTimeSpan = TimeSpan.FromDays(auth.Value.SessionDays);
                options.SlidingExpiration = true;

                // Una API no redirige a una página de login: responde 401 o 403.
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

        string[] cookieSchemes =
        [
            IdentityConstants.ApplicationScheme,
            IdentityConstants.TwoFactorUserIdScheme,
            IdentityConstants.TwoFactorRememberMeScheme,
            IdentityConstants.ExternalScheme,
        ];

        foreach (var scheme in cookieSchemes)
        {
            services.Configure<CookieAuthenticationOptions>(scheme, options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.Path = "/";
            });
        }
    }

    private static void AddAuthorizationPolicies(IServiceCollection services) =>
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Policies.Coach, policy => policy.RequireRole(Roles.Coach))
            .AddPolicy(Policies.Athlete, policy => policy.RequireRole(Roles.Athlete))
            .AddPolicy(Policies.PlatformAdmin, policy => policy
                .RequireRole(Roles.PlatformAdmin)
                .RequireClaim(AuthClaims.AuthenticationMethod, AuthClaims.MultiFactor));
}
