using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RunFitPlatform.Api.Shared.Persistence;
using RunFitPlatform.Api.Shared.Security;

namespace RunFitPlatform.Api.UnitTests.Architecture;

/// <summary>Reglas de persistencia que no expresa ArchUnitNET (secciones 3.3 y 8.7).</summary>
public sealed partial class PersistenceRulesTests
{
    private const string ModulesNamespace = "RunFitPlatform.Api.Modules.";

    /// <summary>
    /// Archivos revisados que pueden desactivar el filtro <c>DataScope</c>. Cada alta en esta lista
    /// necesita revisión explícita en el PR.
    /// </summary>
    private static readonly string[] ReviewedDataScopeBypasses = [];

    [Fact]
    public void EfModel_HasNoNavigationsBetweenModules()
    {
        using var db = CreateDbContext();

        var violations =
            from entity in db.Model.GetEntityTypes()
            let fromModule = ModuleOf(entity.ClrType)
            where fromModule is not null
            from navigation in entity.GetNavigations()
            let toModule = ModuleOf(navigation.TargetEntityType.ClrType)
            where toModule != fromModule
            select $"{entity.ClrType.Name}.{navigation.Name} → {navigation.TargetEntityType.ClrType.Name}";

        Assert.Empty(violations);
    }

    [Fact]
    public void EfModel_EveryTableHasAModuleSchema()
    {
        using var db = CreateDbContext();
        string[] schemas = [Schemas.Identity, Schemas.Coaching, Schemas.Planning, Schemas.TrainingLog, Schemas.Billing, Schemas.Admin];

        var withoutSchema = db.Model.GetEntityTypes()
            .Where(e => e.GetTableName() is not null && !schemas.Contains(e.GetSchema()))
            .Select(e => e.GetTableName());

        Assert.Empty(withoutSchema);
    }

    [Fact]
    public void Source_NeverIgnoresQueryFiltersWithoutNames()
    {
        var offenders = SourceFiles()
            .Where(file => UnnamedIgnoreQueryFilters().IsMatch(File.ReadAllText(file)))
            .Select(Path.GetFileName);

        Assert.Empty(offenders);
    }

    [Fact]
    public void Source_IgnoresDataScopeOnlyInReviewedFiles()
    {
        var offenders = SourceFiles()
            .Where(file => DataScopeBypass().IsMatch(File.ReadAllText(file)))
            .Select(Path.GetFileName)
            .Where(name => !ReviewedDataScopeBypasses.Contains(name));

        Assert.Empty(offenders);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>();
        PersistenceServiceCollectionExtensions.ConfigureDbContext(options, "Host=localhost;Database=model_only");
        return new AppDbContext(options.Options, new AnonymousDataScope());
    }

    private static string? ModuleOf(Type type) =>
        type.Namespace is { } ns && ns.StartsWith(ModulesNamespace, StringComparison.Ordinal)
            ? ns[ModulesNamespace.Length..].Split('.')[0]
            : null;

    private static IEnumerable<string> SourceFiles()
    {
        var src = Path.Combine(RepositoryRoot(), "src");
        return Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "RunFitPlatform.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("No se encontró RunFitPlatform.slnx.");
    }

    [GeneratedRegex(@"IgnoreQueryFilters\s*\(\s*\)")]
    private static partial Regex UnnamedIgnoreQueryFilters();

    [GeneratedRegex(@"IgnoreQueryFilters\s*\([^)]*DataScope")]
    private static partial Regex DataScopeBypass();

    private sealed class AnonymousDataScope : IDataScope
    {
        public Guid? UserId => null;
        public Guid? CoachId => null;
        public Guid? AthleteId => null;
        public bool IsPlatformAdmin => false;
    }
}
