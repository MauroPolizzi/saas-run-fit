using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RunFitPlatform.Api.Shared.Security;

namespace RunFitPlatform.Api.Shared.Persistence;

/// <summary>
/// Único DbContext del monolito. Cada módulo aporta sus configuraciones
/// (<see cref="IEntityTypeConfiguration{TEntity}"/>) en su carpeta <c>Persistence</c>.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options, IDataScope dataScope)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options), IDataProtectionKeyContext
{
    /// <summary>Alcance de datos de la petición actual; lo usan los filtros globales <c>DataScope</c>.</summary>
    internal IDataScope DataScope { get; } = dataScope;

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
