using System.ComponentModel.DataAnnotations;

namespace RunFitPlatform.Api.Shared.Web;

/// <summary>Límites de peticiones por política (sección <c>RateLimiting</c>).</summary>
public sealed class RateLimitingOptions : IValidatableObject
{
    public const string SectionName = "RateLimiting";

    /// <summary>Login: 10 por minuto por IP.</summary>
    public RateLimitRule Login { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };

    /// <summary>Recuperación de contraseña: 5 cada 15 minutos por IP.</summary>
    public RateLimitRule PasswordRecovery { get; set; } = new() { PermitLimit = 5, WindowSeconds = 900 };

    /// <summary>Aceptación de invitaciones: 10 por minuto por IP.</summary>
    public RateLimitRule InvitationAcceptance { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };

    /// <summary>Administración: 60 por minuto por usuario.</summary>
    public RateLimitRule Admin { get; set; } = new() { PermitLimit = 60, WindowSeconds = 60 };

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var (name, rule) in new[]
                 {
                     (nameof(Login), Login),
                     (nameof(PasswordRecovery), PasswordRecovery),
                     (nameof(InvitationAcceptance), InvitationAcceptance),
                     (nameof(Admin), Admin),
                 })
        {
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(rule, new ValidationContext(rule), results, validateAllProperties: true))
            {
                foreach (var result in results)
                {
                    yield return new ValidationResult(
                        $"{name}: {result.ErrorMessage}", result.MemberNames.Select(m => $"{name}.{m}"));
                }
            }
        }
    }
}

public sealed class RateLimitRule
{
    [Range(1, 10_000)]
    public int PermitLimit { get; set; }

    [Range(1, 86_400)]
    public int WindowSeconds { get; set; }
}

/// <summary>Nombres de las políticas de rate limiting.</summary>
public static class RateLimitPolicies
{
    public const string Login = "login";
    public const string PasswordRecovery = "password-recovery";
    public const string InvitationAcceptance = "invitation-acceptance";
    public const string Admin = "admin";
}
