using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RunFitPlatform.Api.Shared.Persistence;
using RunFitPlatform.Api.Shared.Security;

namespace RunFitPlatform.Api.Modules.Accounts.Persistence;

/// <summary>Tablas de Identity en el esquema <c>identity</c>, con los nombres de la sección 8.2.</summary>
internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("users", Schemas.Identity);
        builder.Property(u => u.DisplayName).HasMaxLength(AppUser.DisplayNameMaxLength).IsRequired();

        builder.HasIndex(u => u.NormalizedEmail).IsUnique().HasDatabaseName("ux_users_normalized_email");
        builder.HasIndex(u => u.NormalizedUserName).IsUnique().HasDatabaseName("ux_users_normalized_user_name");
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    /// <summary>Ids fijos: los roles se crean con la migración inicial.</summary>
    public static readonly IdentityRole<Guid>[] SeedRoles =
    [
        Role("019a3a10-0000-7000-8000-000000000001", Roles.Coach),
        Role("019a3a10-0000-7000-8000-000000000002", Roles.Athlete),
        Role("019a3a10-0000-7000-8000-000000000003", Roles.PlatformAdmin),
    ];

    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> builder)
    {
        builder.ToTable("roles", Schemas.Identity);
        builder.HasIndex(r => r.NormalizedName).IsUnique().HasDatabaseName("ux_roles_normalized_name");
        builder.HasData(SeedRoles);
    }

    private static IdentityRole<Guid> Role(string id, string name) => new()
    {
        Id = Guid.Parse(id),
        Name = name,
        NormalizedName = name.ToUpperInvariant(),
        ConcurrencyStamp = id,
    };
}

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<IdentityUserRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> builder) =>
        builder.ToTable("user_roles", Schemas.Identity);
}

internal sealed class UserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder) =>
        builder.ToTable("user_claims", Schemas.Identity);
}

internal sealed class UserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder) =>
        builder.ToTable("user_logins", Schemas.Identity);
}

internal sealed class UserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> builder) =>
        builder.ToTable("user_tokens", Schemas.Identity);
}

internal sealed class RoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> builder)
    {
        builder.ToTable("role_claims", Schemas.Identity);
        builder.HasOne<IdentityRole<Guid>>().WithMany().HasForeignKey(rc => rc.RoleId)
            .HasConstraintName("fk_role_claims_roles_role_id");
    }
}

internal sealed class DataProtectionKeyConfiguration : IEntityTypeConfiguration<DataProtectionKey>
{
    public void Configure(EntityTypeBuilder<DataProtectionKey> builder) =>
        builder.ToTable("data_protection_keys", Schemas.Identity);
}
