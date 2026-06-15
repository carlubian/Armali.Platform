using System.Reflection;

namespace Segaris.ArchitectureTests;

/// <summary>
/// Enforces the Capex/Configuration/Launcher dependency direction inside the
/// single <c>Segaris.Api</c> assembly. The modules live in sibling namespaces
/// rather than separate assemblies, so the boundary is verified by inspecting
/// the signature surface (base types, interfaces, fields, properties,
/// constructor and method signatures) of every type in a module for references
/// into a namespace it must not depend on.
/// </summary>
public sealed class ModuleBoundaryTests
{
    private const string ConfigurationNamespace = "Segaris.Api.Modules.Configuration";
    private const string CapexNamespace = "Segaris.Api.Modules.Capex";
    private const string LauncherNamespace = "Segaris.Api.Modules.Launcher";

    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    [Fact]
    public void Module_namespaces_are_populated()
    {
        // Guards the boundary tests below from silently passing if a namespace
        // is renamed or emptied.
        Assert.NotEmpty(TypesIn(ConfigurationNamespace));
        Assert.NotEmpty(TypesIn(CapexNamespace));
        Assert.NotEmpty(TypesIn(LauncherNamespace));
    }

    [Fact]
    public void Configuration_does_not_depend_on_capex()
    {
        AssertNoDependency(ConfigurationNamespace, CapexNamespace);
    }

    [Fact]
    public void Capex_depends_on_configuration_contracts()
    {
        // The accepted dependency direction is Capex -> Configuration. Capex
        // consumes the shared catalog contracts (reads now, reference-management
        // handlers later), so this dependency must remain present; its absence
        // would mean the boundary was inverted or the consumer contract moved.
        var dependsOnConfiguration = TypesIn(CapexNamespace)
            .SelectMany(ReferencedTypes)
            .Any(referenced => IsInNamespace(referenced, ConfigurationNamespace));

        Assert.True(
            dependsOnConfiguration,
            "Capex must depend on Configuration's published catalog contracts.");
    }

    [Fact]
    public void Reference_management_contract_is_owned_by_configuration()
    {
        // The reference-migration handler interface is the cross-module seam.
        // Keeping it in the Configuration namespace is what preserves the
        // Capex -> Configuration direction when Capex implements handlers.
        var handler = ApiAssembly.GetType(
            "Segaris.Api.Modules.Configuration.Contracts.ICatalogReferenceHandler",
            throwOnError: false);

        Assert.NotNull(handler);
        Assert.True(IsInNamespace(handler!, ConfigurationNamespace));
    }

    [Fact]
    public void Configuration_does_not_depend_on_launcher()
    {
        AssertNoDependency(ConfigurationNamespace, LauncherNamespace);
    }

    [Fact]
    public void Launcher_does_not_depend_on_business_modules()
    {
        // The Launcher aggregates attention through ILauncherAttentionContributor
        // and must not reference the Capex or Configuration namespaces directly.
        AssertNoDependency(LauncherNamespace, CapexNamespace);
        AssertNoDependency(LauncherNamespace, ConfigurationNamespace);
    }

    private static void AssertNoDependency(string sourceNamespace, string forbiddenNamespace)
    {
        var violations = TypesIn(sourceNamespace)
            .SelectMany(type => ReferencedTypes(type)
                .Where(referenced => IsInNamespace(referenced, forbiddenNamespace))
                .Select(referenced => $"{type.FullName} -> {referenced.FullName}"))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Types in '{sourceNamespace}' must not depend on '{forbiddenNamespace}':"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    private static List<Type> TypesIn(string @namespace) =>
        ApiAssembly.GetTypes()
            .Where(type => type.Namespace is { } ns
                && (ns.Equals(@namespace, StringComparison.Ordinal)
                    || ns.StartsWith(@namespace + ".", StringComparison.Ordinal)))
            .ToList();

    private static bool IsInNamespace(Type type, string @namespace) =>
        type.Namespace is { } ns
            && (ns.Equals(@namespace, StringComparison.Ordinal)
                || ns.StartsWith(@namespace + ".", StringComparison.Ordinal));

    private static IEnumerable<Type> ReferencedTypes(Type type)
    {
        const BindingFlags members = BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.DeclaredOnly;

        var referenced = new List<Type>();

        if (type.BaseType is not null)
        {
            referenced.Add(type.BaseType);
        }

        referenced.AddRange(type.GetInterfaces());
        referenced.AddRange(type.GetFields(members).Select(field => field.FieldType));
        referenced.AddRange(type.GetProperties(members).Select(property => property.PropertyType));

        foreach (var constructor in type.GetConstructors(members))
        {
            referenced.AddRange(constructor.GetParameters().Select(parameter => parameter.ParameterType));
        }

        foreach (var method in type.GetMethods(members))
        {
            referenced.Add(method.ReturnType);
            referenced.AddRange(method.GetParameters().Select(parameter => parameter.ParameterType));
        }

        return referenced.SelectMany(Expand);
    }

    private static IEnumerable<Type> Expand(Type? type)
    {
        if (type is null)
        {
            yield break;
        }

        if (type.HasElementType)
        {
            foreach (var element in Expand(type.GetElementType()))
            {
                yield return element;
            }

            yield break;
        }

        yield return type;

        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                foreach (var expanded in Expand(argument))
                {
                    yield return expanded;
                }
            }
        }
    }
}
