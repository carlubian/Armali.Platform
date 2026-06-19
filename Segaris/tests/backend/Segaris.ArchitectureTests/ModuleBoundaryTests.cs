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
    private const string OpexNamespace = "Segaris.Api.Modules.Opex";
    private const string InventoryNamespace = "Segaris.Api.Modules.Inventory";
    private const string TravelNamespace = "Segaris.Api.Modules.Travel";
    private const string ClothesNamespace = "Segaris.Api.Modules.Clothes";
    private const string AssetsNamespace = "Segaris.Api.Modules.Assets";
    private const string AssetsContractsNamespace = "Segaris.Api.Modules.Assets.Contracts";
    private const string MoodNamespace = "Segaris.Api.Modules.Mood";
    private const string MaintenanceNamespace = "Segaris.Api.Modules.Maintenance";

    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    [Fact]
    public void Module_namespaces_are_populated()
    {
        // Guards the boundary tests below from silently passing if a namespace
        // is renamed or emptied.
        Assert.NotEmpty(TypesIn(ConfigurationNamespace));
        Assert.NotEmpty(TypesIn(CapexNamespace));
        Assert.NotEmpty(TypesIn(LauncherNamespace));
        Assert.NotEmpty(TypesIn(OpexNamespace));
        Assert.NotEmpty(TypesIn(InventoryNamespace));
        Assert.NotEmpty(TypesIn(TravelNamespace));
        Assert.NotEmpty(TypesIn(ClothesNamespace));
        Assert.NotEmpty(TypesIn(AssetsNamespace));
        Assert.NotEmpty(TypesIn(MoodNamespace));
        Assert.NotEmpty(TypesIn(MaintenanceNamespace));
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
    public void Configuration_does_not_depend_on_opex()
    {
        AssertNoDependency(ConfigurationNamespace, OpexNamespace);
    }

    [Fact]
    public void Opex_depends_on_configuration_contracts()
    {
        var dependsOnConfiguration = TypesIn(OpexNamespace)
            .SelectMany(ReferencedTypes)
            .Any(referenced => IsInNamespace(referenced, ConfigurationNamespace));

        Assert.True(
            dependsOnConfiguration,
            "Opex must depend on Configuration's published catalog contracts.");
    }

    [Fact]
    public void Opex_does_not_depend_on_other_business_modules()
    {
        AssertNoDependency(OpexNamespace, CapexNamespace);
        AssertNoDependency(OpexNamespace, MaintenanceNamespace);
    }

    [Fact]
    public void Configuration_does_not_depend_on_inventory()
    {
        AssertNoDependency(ConfigurationNamespace, InventoryNamespace);
    }

    [Fact]
    public void Inventory_depends_on_configuration_contracts()
    {
        var dependsOnConfiguration = TypesIn(InventoryNamespace)
            .SelectMany(ReferencedTypes)
            .Any(referenced => IsInNamespace(referenced, ConfigurationNamespace));

        Assert.True(
            dependsOnConfiguration,
            "Inventory must depend on Configuration's published catalog contracts.");
    }

    [Fact]
    public void Inventory_does_not_depend_on_other_business_modules()
    {
        AssertNoDependency(InventoryNamespace, CapexNamespace);
        AssertNoDependency(InventoryNamespace, OpexNamespace);
        AssertNoDependency(InventoryNamespace, TravelNamespace);
        AssertNoDependency(InventoryNamespace, MaintenanceNamespace);
    }

    [Fact]
    public void Configuration_does_not_depend_on_travel()
    {
        AssertNoDependency(ConfigurationNamespace, TravelNamespace);
    }

    [Fact]
    public void Travel_depends_on_configuration_contracts()
    {
        var dependsOnConfiguration = TypesIn(TravelNamespace)
            .SelectMany(ReferencedTypes)
            .Any(referenced => IsInNamespace(referenced, ConfigurationNamespace));

        Assert.True(
            dependsOnConfiguration,
            "Travel must depend on Configuration's published catalog contracts.");
    }

    [Fact]
    public void Travel_does_not_depend_on_other_business_modules()
    {
        AssertNoDependency(TravelNamespace, CapexNamespace);
        AssertNoDependency(TravelNamespace, OpexNamespace);
        AssertNoDependency(TravelNamespace, InventoryNamespace);
        AssertNoDependency(TravelNamespace, ClothesNamespace);
        AssertNoDependency(TravelNamespace, AssetsNamespace);
        AssertNoDependency(TravelNamespace, MoodNamespace);
        AssertNoDependency(TravelNamespace, MaintenanceNamespace);
    }

    [Fact]
    public void Configuration_does_not_depend_on_clothes()
    {
        AssertNoDependency(ConfigurationNamespace, ClothesNamespace);
    }

    [Fact]
    public void Clothes_depends_on_configuration_contracts()
    {
        var dependsOnConfiguration = TypesIn(ClothesNamespace)
            .SelectMany(ReferencedTypes)
            .Any(referenced => IsInNamespace(referenced, ConfigurationNamespace));

        Assert.True(
            dependsOnConfiguration,
            "Clothes must depend on Configuration's published catalog contracts.");
    }

    [Fact]
    public void Clothes_does_not_depend_on_other_business_modules()
    {
        AssertNoDependency(ClothesNamespace, CapexNamespace);
        AssertNoDependency(ClothesNamespace, OpexNamespace);
        AssertNoDependency(ClothesNamespace, InventoryNamespace);
        AssertNoDependency(ClothesNamespace, TravelNamespace);
        AssertNoDependency(ClothesNamespace, AssetsNamespace);
        AssertNoDependency(ClothesNamespace, MoodNamespace);
        AssertNoDependency(ClothesNamespace, MaintenanceNamespace);
    }

    [Fact]
    public void Clothes_does_not_depend_on_launcher()
    {
        AssertNoDependency(ClothesNamespace, LauncherNamespace);
    }

    [Fact]
    public void Configuration_does_not_depend_on_assets()
    {
        AssertNoDependency(ConfigurationNamespace, AssetsNamespace);
    }

    [Fact]
    public void Assets_depends_on_configuration_contracts()
    {
        var dependsOnConfiguration = TypesIn(AssetsNamespace)
            .SelectMany(ReferencedTypes)
            .Any(referenced => IsInNamespace(referenced, ConfigurationNamespace));

        Assert.True(
            dependsOnConfiguration,
            "Assets must depend on Configuration's published catalog contracts.");
    }

    [Fact]
    public void Assets_does_not_depend_on_other_business_modules()
    {
        // Assets contributes launcher attention, so it may reference the Launcher
        // contracts; it must remain independent from every other business module.
        AssertNoDependency(AssetsNamespace, CapexNamespace);
        AssertNoDependency(AssetsNamespace, OpexNamespace);
        AssertNoDependency(AssetsNamespace, InventoryNamespace);
        AssertNoDependency(AssetsNamespace, TravelNamespace);
        AssertNoDependency(AssetsNamespace, ClothesNamespace);
        AssertNoDependency(AssetsNamespace, MoodNamespace);
    }

    [Fact]
    public void Configuration_does_not_depend_on_mood()
    {
        AssertNoDependency(ConfigurationNamespace, MoodNamespace);
    }

    [Fact]
    public void Mood_does_not_depend_on_configuration()
    {
        // Mood is privacy-first and owns its fixed criteria and derived-emotion
        // matrix. Unlike the other business modules it must not consume any
        // Configuration catalog or reference-management contract.
        AssertNoDependency(MoodNamespace, ConfigurationNamespace);
    }

    [Fact]
    public void Mood_does_not_depend_on_other_business_modules()
    {
        AssertNoDependency(MoodNamespace, CapexNamespace);
        AssertNoDependency(MoodNamespace, OpexNamespace);
        AssertNoDependency(MoodNamespace, InventoryNamespace);
        AssertNoDependency(MoodNamespace, TravelNamespace);
        AssertNoDependency(MoodNamespace, ClothesNamespace);
        AssertNoDependency(MoodNamespace, AssetsNamespace);
        AssertNoDependency(MoodNamespace, MaintenanceNamespace);
    }

    [Fact]
    public void Mood_does_not_depend_on_launcher()
    {
        // Mood contributes no launcher attention, so it must not reference the
        // Launcher namespace at all.
        AssertNoDependency(MoodNamespace, LauncherNamespace);
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
        AssertNoDependency(LauncherNamespace, OpexNamespace);
        AssertNoDependency(LauncherNamespace, InventoryNamespace);
        AssertNoDependency(LauncherNamespace, TravelNamespace);
        AssertNoDependency(LauncherNamespace, ClothesNamespace);
        AssertNoDependency(LauncherNamespace, AssetsNamespace);
        AssertNoDependency(LauncherNamespace, MoodNamespace);
        AssertNoDependency(LauncherNamespace, MaintenanceNamespace);
    }

    [Fact]
    public void Configuration_does_not_depend_on_maintenance()
    {
        AssertNoDependency(ConfigurationNamespace, MaintenanceNamespace);
    }

    [Fact]
    public void Maintenance_depends_on_configuration_contracts()
    {
        // Maintenance owns the MaintenanceType catalogue surfaced through
        // Configuration, so it consumes Configuration's published contracts. Its
        // absence would mean the catalogue-presentation boundary was inverted.
        var dependsOnConfiguration = TypesIn(MaintenanceNamespace)
            .SelectMany(ReferencedTypes)
            .Any(referenced => IsInNamespace(referenced, ConfigurationNamespace));

        Assert.True(
            dependsOnConfiguration,
            "Maintenance must depend on Configuration's published catalog contracts.");
    }

    [Fact]
    public void Maintenance_does_not_depend_on_other_business_modules()
    {
        // Maintenance is the first business module that references another business
        // module: it may consume Assets (the optional live reference and the deletion
        // guard) and contributes launcher attention, so it may reference the Assets
        // and Launcher namespaces. It must remain independent from every other
        // business module.
        AssertNoDependency(MaintenanceNamespace, CapexNamespace);
        AssertNoDependency(MaintenanceNamespace, OpexNamespace);
        AssertNoDependency(MaintenanceNamespace, InventoryNamespace);
        AssertNoDependency(MaintenanceNamespace, TravelNamespace);
        AssertNoDependency(MaintenanceNamespace, ClothesNamespace);
        AssertNoDependency(MaintenanceNamespace, MoodNamespace);
    }

    [Fact]
    public void Maintenance_may_only_consume_assets_through_published_contracts()
    {
        // The Maintenance -> Assets seam is the first business-to-business
        // dependency, so it is policed more strictly than the Configuration seam:
        // Maintenance may reference only the Assets.Contracts namespace (the read
        // contract and the deletion-reference contract) and never Assets domain,
        // persistence, queries, or mutations.
        var violations = TypesIn(MaintenanceNamespace)
            .SelectMany(type => ReferencedTypes(type)
                .Where(referenced => IsInNamespace(referenced, AssetsNamespace)
                    && !IsInNamespace(referenced, AssetsContractsNamespace))
                .Select(referenced => $"{type.FullName} -> {referenced.FullName}"))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Maintenance may consume Assets only through 'Assets.Contracts':"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Assets_does_not_depend_on_maintenance()
    {
        // The deletion guard is implemented by contract inversion: Assets defines the
        // read and deletion-reference contracts, Maintenance implements them, and
        // Assets enumerates registered handlers without ever referencing Maintenance.
        // This keeps the dependency direction Maintenance -> Assets.
        AssertNoDependency(AssetsNamespace, MaintenanceNamespace);
    }

    [Fact]
    public void Assets_publishes_the_cross_module_reference_contracts()
    {
        // The read contract and the deletion-reference contract are the cross-module
        // seam. Keeping them in the Assets namespace is what preserves the
        // Maintenance -> Assets direction when Maintenance consumes and implements
        // them.
        var reader = ApiAssembly.GetType(
            "Segaris.Api.Modules.Assets.Contracts.IAssetReferenceReader",
            throwOnError: false);
        var deletionHandler = ApiAssembly.GetType(
            "Segaris.Api.Modules.Assets.Contracts.IAssetDeletionReferenceHandler",
            throwOnError: false);

        Assert.NotNull(reader);
        Assert.NotNull(deletionHandler);
        Assert.True(IsInNamespace(reader!, AssetsContractsNamespace));
        Assert.True(IsInNamespace(deletionHandler!, AssetsContractsNamespace));
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
