namespace RunFitPlatform.Api.Shared.Persistence;

/// <summary>
/// Nombres de los filtros globales de EF Core. Desactivar un filtro exige nombrarlo, por ejemplo
/// <c>IgnoreQueryFilters([QueryFilters.NotArchived])</c>. La sobrecarga sin nombres está prohibida
/// (lo verifica un test) y <see cref="DataScope"/> solo se ignora en casos revisados.
/// </summary>
public static class QueryFilters
{
    public const string DataScope = "DataScope";
    public const string NotArchived = "NotArchived";
}
