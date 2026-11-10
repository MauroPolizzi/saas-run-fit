using Microsoft.AspNetCore.Identity;

namespace RunFitPlatform.Api.Shared.Security;

/// <summary>
/// Usuario de ASP.NET Core Identity. Vive en <c>Shared</c> porque lo necesita <c>AppDbContext</c>
/// y <c>Shared</c> no depende de ningún módulo. Sus tablas (<c>identity.*</c>) las configura y modifica
/// solo el módulo Accounts.
/// </summary>
public sealed class AppUser : IdentityUser<Guid>
{
    public const int DisplayNameMaxLength = 80;

    public AppUser()
    {
        Id = Guid.CreateVersion7();
    }

    public string DisplayName { get; set; } = string.Empty;

    public bool IsDisabled { get; set; }

    public DateTimeOffset? LastSeenAt { get; set; }
}
