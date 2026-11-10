using System.ComponentModel.DataAnnotations;

namespace RunFitPlatform.Api.Shared.Security;

/// <summary>Sesiones, invitaciones y bloqueo de cuentas (sección <c>Auth</c>).</summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Duración deslizante de la sesión con "Recordarme".</summary>
    [Range(1, 90)]
    public int SessionDays { get; set; } = 14;

    /// <summary>Duración absoluta de la sesión del administrador, sin renovación.</summary>
    [Range(1, 24)]
    public int AdminSessionHours { get; set; } = 8;

    [Range(1, 30)]
    public int InvitationExpiryDays { get; set; } = 7;

    [Range(1, 20)]
    public int MaxFailedAccessAttempts { get; set; } = 5;

    [Range(1, 1440)]
    public int LockoutMinutes { get; set; } = 15;

    /// <summary>Intervalo de revalidación del security stamp.</summary>
    [Range(1, 60)]
    public int SecurityStampValidationMinutes { get; set; } = 5;
}
