using System.Security.Claims;

namespace RunFitPlatform.Api.Shared.Security;

/// <summary>
/// Alcance de datos de la petición, resuelto desde la sesión. Lo usan los filtros globales <c>DataScope</c>
/// y los casos de uso. <c>CoachId</c> y <c>AthleteId</c> coinciden con el id del usuario.
/// </summary>
/// <remarks>
/// Los ids de relaciones activas del alumno (<c>CoachAthleteIds</c>) se agregan en el Hito 1, cuando exista
/// el módulo Coaching que los provee.
/// </remarks>
public interface IDataScope
{
    Guid? UserId { get; }

    /// <summary>Id del entrenador si la sesión tiene el rol <c>Coach</c>.</summary>
    Guid? CoachId { get; }

    /// <summary>Id del alumno si la sesión tiene el rol <c>Athlete</c>.</summary>
    Guid? AthleteId { get; }

    bool IsPlatformAdmin { get; }
}

/// <summary>Implementación basada en el usuario autenticado de la petición HTTP.</summary>
public sealed class HttpDataScope(IHttpContextAccessor httpContextAccessor) : IDataScope
{
    private readonly Lazy<Resolved> _resolved = new(() => Resolve(httpContextAccessor.HttpContext?.User));

    public Guid? UserId => _resolved.Value.UserId;
    public Guid? CoachId => _resolved.Value.CoachId;
    public Guid? AthleteId => _resolved.Value.AthleteId;
    public bool IsPlatformAdmin => _resolved.Value.IsPlatformAdmin;

    internal static Resolved Resolve(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true
            || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Resolved.Anonymous;
        }

        return new Resolved(
            userId,
            user.IsInRole(Roles.Coach) ? userId : null,
            user.IsInRole(Roles.Athlete) ? userId : null,
            user.IsInRole(Roles.PlatformAdmin));
    }

    internal sealed record Resolved(Guid? UserId, Guid? CoachId, Guid? AthleteId, bool IsPlatformAdmin)
    {
        public static readonly Resolved Anonymous = new(null, null, null, false);
    }
}
