using Microsoft.AspNetCore.Identity;

namespace RunFitPlatform.Api.Shared.Security;

/// <summary>Identity solo valida el largo mínimo; esta regla agrega el máximo de 128 caracteres.</summary>
public sealed class MaxLengthPasswordValidator : IPasswordValidator<AppUser>
{
    public const int MaxLength = 128;

    public Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user, string? password)
    {
        if (password is { Length: > MaxLength })
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordTooLong",
                Description = $"La contraseña no puede superar {MaxLength} caracteres.",
            }));
        }

        return Task.FromResult(IdentityResult.Success);
    }
}
