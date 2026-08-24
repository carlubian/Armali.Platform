using System.Reflection;
using Blackwing.Persistence;
using Blackwing.Shared;

namespace Blackwing.ArchitectureTests;

/// <summary>
/// Pins the dependency direction fixed for this phase: Api to Persistence to Shared, never the
/// other way round. Inverting any reference must break the build of these tests.
/// </summary>
public sealed class DependencyTests
{
    private const string ApiAssemblyName = "Blackwing.Api";
    private const string PersistenceAssemblyName = "Blackwing.Persistence";

    [Fact]
    public void Shared_does_not_reference_the_api_or_the_persistence_layer()
    {
        var references = GetReferencedAssemblyNames(SharedAssembly.Assembly);

        Assert.DoesNotContain(ApiAssemblyName, references);
        Assert.DoesNotContain(PersistenceAssemblyName, references);
    }

    [Fact]
    public void Persistence_does_not_reference_the_api()
    {
        var references = GetReferencedAssemblyNames(PersistenceAssembly.Assembly);

        Assert.DoesNotContain(ApiAssemblyName, references);
    }

    [Fact]
    public void Persistence_public_surface_does_not_expose_api_types()
    {
        var violations = PersistenceAssembly.Assembly
            .GetExportedTypes()
            .SelectMany(DescribeSurfaceTypes)
            .Where(entry => IsApiType(entry.Type))
            .Select(entry => $"{entry.Member} exposes {entry.Type.FullName}")
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void Api_is_the_executable_composition_root()
    {
        var apiAssembly = typeof(Program).Assembly;

        Assert.Equal(ApiAssemblyName, apiAssembly.GetName().Name);
        Assert.NotNull(apiAssembly.EntryPoint);
    }

    private static string?[] GetReferencedAssemblyNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();

    private static bool IsApiType(Type type)
    {
        var candidate = type.IsByRef || type.IsPointer || type.IsArray
            ? type.GetElementType() ?? type
            : type;

        if (candidate.IsGenericType)
        {
            return candidate.GetGenericArguments().Any(IsApiType)
                || IsApiAssembly(candidate.GetGenericTypeDefinition());
        }

        return IsApiAssembly(candidate);
    }

    private static bool IsApiAssembly(Type type) =>
        string.Equals(type.Assembly.GetName().Name, ApiAssemblyName, StringComparison.Ordinal);

    /// <summary>
    /// Yields every type reachable through the public and protected surface of <paramref name="type"/>:
    /// base type, interfaces, member signatures, and their generic arguments.
    /// </summary>
    private static IEnumerable<(string Member, Type Type)> DescribeSurfaceTypes(Type type)
    {
        const BindingFlags Surface = BindingFlags.Public
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.DeclaredOnly;

        if (type.BaseType is not null)
        {
            yield return ($"{type.FullName} base type", type.BaseType);
        }

        foreach (var contract in type.GetInterfaces())
        {
            yield return ($"{type.FullName} interface", contract);
        }

        foreach (var argument in type.IsGenericType ? type.GetGenericArguments() : [])
        {
            yield return ($"{type.FullName} generic argument", argument);
        }

        foreach (var property in type.GetProperties(Surface))
        {
            yield return ($"{type.FullName}.{property.Name}", property.PropertyType);
        }

        foreach (var field in type.GetFields(Surface))
        {
            yield return ($"{type.FullName}.{field.Name}", field.FieldType);
        }

        foreach (var method in type.GetMethods(Surface))
        {
            yield return ($"{type.FullName}.{method.Name}", method.ReturnType);
            foreach (var parameter in method.GetParameters())
            {
                yield return ($"{type.FullName}.{method.Name}", parameter.ParameterType);
            }
        }

        foreach (var constructor in type.GetConstructors(Surface))
        {
            foreach (var parameter in constructor.GetParameters())
            {
                yield return ($"{type.FullName}..ctor", parameter.ParameterType);
            }
        }
    }
}
