using ArchUnitNET.Fluent.Syntax.Elements.Types;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static ArchUnitNET.Fluent.Slices.SliceRuleDefinition;

namespace RunFitPlatform.Api.UnitTests.Architecture;

/// <summary>
/// Reglas de módulos de la sección 3.3 de <c>docs/design/technical-design.md</c> (ADR-004).
/// Si una falla, se corrige el código, no el test.
/// </summary>
public sealed class ModuleRulesTests
{
    private const string Root = "RunFitPlatform.Api";
    private const string ModulesRoot = Root + ".Modules.";

    private static readonly ArchUnitNET.Domain.Architecture Architecture =
        new ArchLoader().LoadAssemblies(typeof(Program).Assembly).Build();

    public static readonly string[] Modules =
        ["Accounts", "Coaching", "Planning", "TrainingLog", "Progress", "Billing", "Admin"];

    /// <summary>Dependencias permitidas entre módulos (regla 2 de CLAUDE.md). Todos pueden usar Shared.</summary>
    private static readonly Dictionary<string, string[]> AllowedDependencies = new()
    {
        ["Accounts"] = [],
        ["Coaching"] = ["Accounts", "Billing"],
        ["Planning"] = ["Coaching", "Billing"],
        ["TrainingLog"] = ["Planning", "Coaching"],
        ["Progress"] = ["Coaching", "TrainingLog", "Planning"],
        ["Billing"] = [],
        ["Admin"] = ["Accounts", "Coaching", "Billing"],
    };

    /// <summary>Excepción documentada: Progress lee entidades de Domain de TrainingLog y Planning.</summary>
    private static readonly HashSet<(string From, string To)> DomainReadExceptions =
    [
        ("Progress", "TrainingLog"),
        ("Progress", "Planning"),
    ];

    public static TheoryData<string, string> ModulePairs()
    {
        var data = new TheoryData<string, string>();
        foreach (var from in Modules)
        {
            foreach (var to in Modules.Where(m => m != from))
            {
                data.Add(from, to);
            }
        }

        return data;
    }

    public static TheoryData<string> ModuleNames() => new(Modules);

    [Theory]
    [MemberData(nameof(ModulePairs))]
    public void Module_DependsOnlyOnAllowedModules(string from, string to)
    {
        if (AllowedDependencies[from].Contains(to))
        {
            return;
        }

        TypesOf(from).Should().NotDependOnAny(TypesOf(to))
            .Because($"{from} no tiene permitido depender de {to}")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    [Theory]
    [MemberData(nameof(ModulePairs))]
    public void Module_UsesOtherModuleOnlyThroughContracts(string from, string to)
    {
        if (!AllowedDependencies[from].Contains(to))
        {
            return;
        }

        var internals = Types().That().HaveFullNameStartingWith(ModulesRoot + to + ".")
            .And().DoNotHaveFullNameStartingWith(ModulesRoot + to + ".Contracts.");

        if (DomainReadExceptions.Contains((from, to)))
        {
            internals = internals.And().DoNotHaveFullNameStartingWith(ModulesRoot + to + ".Domain.");
        }

        TypesOf(from).Should().NotDependOnAny(internals)
            .Because($"{from} solo puede usar la carpeta Contracts de {to}")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    [Fact]
    public void Admin_DoesNotDependOnHealthTypes()
    {
        TypesOf("Admin").Should().NotDependOnAny(
                Types().That().HaveFullNameStartingWith(ModulesRoot + "Coaching.")
                    .And().HaveFullNameContaining("Health"))
            .Because("el administrador no ve datos de salud (ADR-009, ADR-010)")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Domain_DoesNotDependOnAspNetCoreOrEntityFramework(string module)
    {
        Types().That().HaveFullNameStartingWith(ModulesRoot + module + ".Domain.")
            .Should().NotDependOnAny(
                Types().That().HaveFullNameStartingWith("Microsoft.AspNetCore.")
                    .Or().HaveFullNameStartingWith("Microsoft.EntityFrameworkCore."))
            .Because("el dominio no conoce la web ni la persistencia")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    [Fact]
    public void Shared_DoesNotDependOnModules()
    {
        Types().That().HaveFullNameStartingWith(Root + ".Shared.")
            .Should().NotDependOnAny(Types().That().HaveFullNameStartingWith(ModulesRoot))
            .Because("Shared es infraestructura transversal y no conoce a los módulos")
            .Check(Architecture);
    }

    [Fact]
    public void Modules_AreFreeOfCycles()
    {
        Slices().Matching(ModulesRoot + "(*)..").Should().BeFreeOfCycles()
            .Check(Architecture);
    }

    private static GivenTypesConjunction TypesOf(string module) =>
        Types().That().HaveFullNameStartingWith(ModulesRoot + module + ".");
}
