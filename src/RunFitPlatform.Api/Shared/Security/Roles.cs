namespace RunFitPlatform.Api.Shared.Security;

/// <summary>Roles de la plataforma. <see cref="PlatformAdmin"/> solo se asigna por línea de comandos.</summary>
public static class Roles
{
    public const string Coach = "Coach";
    public const string Athlete = "Athlete";
    public const string PlatformAdmin = "PlatformAdmin";
}

/// <summary>Políticas de autorización por perspectiva de la API.</summary>
public static class Policies
{
    public const string Coach = "Coach";
    public const string Athlete = "Athlete";
    public const string PlatformAdmin = "PlatformAdmin";
}

/// <summary>Claims de autenticación usados por las políticas.</summary>
public static class AuthClaims
{
    /// <summary>
    /// Método de autenticación. Identity agrega <c>amr = mfa</c> al completar el segundo factor
    /// (<c>SignInManager.TwoFactorSignInAsync</c>). Se confirma con un test al implementar el login con 2FA (Hito 1).
    /// </summary>
    public const string AuthenticationMethod = "amr";
    public const string MultiFactor = "mfa";
}
