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
    private const string ConfigurationContractsNamespace = "Segaris.Api.Modules.Configuration.Contracts";
    private const string AnalyticsNamespace = "Segaris.Api.Modules.Analytics";
    private const string CapexNamespace = "Segaris.Api.Modules.Capex";
    private const string CapexContractsNamespace = "Segaris.Api.Modules.Capex.Contracts";
    private const string CalendarNamespace = "Segaris.Api.Modules.Calendar";
    private const string LauncherNamespace = "Segaris.Api.Modules.Launcher";
    private const string OpexNamespace = "Segaris.Api.Modules.Opex";
    private const string OpexContractsNamespace = "Segaris.Api.Modules.Opex.Contracts";
    private const string InventoryNamespace = "Segaris.Api.Modules.Inventory";
    private const string InventoryContractsNamespace = "Segaris.Api.Modules.Inventory.Contracts";
    private const string TravelNamespace = "Segaris.Api.Modules.Travel";
    private const string TravelContractsNamespace = "Segaris.Api.Modules.Travel.Contracts";
    private const string ClothesNamespace = "Segaris.Api.Modules.Clothes";
    private const string AssetsNamespace = "Segaris.Api.Modules.Assets";
    private const string AssetsContractsNamespace = "Segaris.Api.Modules.Assets.Contracts";
    private const string MoodNamespace = "Segaris.Api.Modules.Mood";
    private const string MaintenanceNamespace = "Segaris.Api.Modules.Maintenance";
    private const string MaintenanceContractsNamespace = "Segaris.Api.Modules.Maintenance.Contracts";
    private const string ProjectsNamespace = "Segaris.Api.Modules.Projects";
    private const string ProcessesNamespace = "Segaris.Api.Modules.Processes";
    private const string ProcessesContractsNamespace = "Segaris.Api.Modules.Processes.Contracts";
    private const string FirebirdNamespace = "Segaris.Api.Modules.Firebird";
    private const string FirebirdContractsNamespace = "Segaris.Api.Modules.Firebird.Contracts";
    private const string RecipesNamespace = "Segaris.Api.Modules.Recipes";
    private const string DestinationsNamespace = "Segaris.Api.Modules.Destinations";
    private const string DestinationsContractsNamespace = "Segaris.Api.Modules.Destinations.Contracts";
    private const string HealthNamespace = "Segaris.Api.Modules.Health";
    private const string HealthContractsNamespace = "Segaris.Api.Modules.Health.Contracts";
    private const string GamesNamespace = "Segaris.Api.Modules.Games";
    private const string GamesContractsNamespace = "Segaris.Api.Modules.Games.Contracts";

    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    [Fact]
    public void Module_namespaces_are_populated()
    {
        // Guards the boundary tests below from silently passing if a namespace
        // is renamed or emptied.
        Assert.NotEmpty(TypesIn(ConfigurationNamespace));
        Assert.NotEmpty(TypesIn(AnalyticsNamespace));
        Assert.NotEmpty(TypesIn(CapexNamespace));
        Assert.NotEmpty(TypesIn(CalendarNamespace));
        Assert.NotEmpty(TypesIn(LauncherNamespace));
        Assert.NotEmpty(TypesIn(OpexNamespace));
        Assert.NotEmpty(TypesIn(InventoryNamespace));
        Assert.NotEmpty(TypesIn(TravelNamespace));
        Assert.NotEmpty(TypesIn(ClothesNamespace));
        Assert.NotEmpty(TypesIn(AssetsNamespace));
        Assert.NotEmpty(TypesIn(MoodNamespace));
        Assert.NotEmpty(TypesIn(MaintenanceNamespace));
        Assert.NotEmpty(TypesIn(ProjectsNamespace));
        Assert.NotEmpty(TypesIn(ProcessesNamespace));
        Assert.NotEmpty(TypesIn(FirebirdNamespace));
        Assert.NotEmpty(TypesIn(RecipesNamespace));
        Assert.NotEmpty(TypesIn(DestinationsNamespace));
        Assert.NotEmpty(TypesIn(HealthNamespace));
        Assert.NotEmpty(TypesIn(GamesNamespace));
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
        AssertNoDependency(InventoryNamespace, HealthNamespace);
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
        AssertNoDependency(TravelNamespace, RecipesNamespace);
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
        AssertNoDependency(LauncherNamespace, ProjectsNamespace);
        AssertNoDependency(LauncherNamespace, ProcessesNamespace);
        AssertNoDependency(LauncherNamespace, FirebirdNamespace);
        AssertNoDependency(LauncherNamespace, RecipesNamespace);
        AssertNoDependency(LauncherNamespace, DestinationsNamespace);
        AssertNoDependency(LauncherNamespace, HealthNamespace);
        AssertNoDependency(LauncherNamespace, CalendarNamespace);
        AssertNoDependency(LauncherNamespace, AnalyticsNamespace);
        AssertNoDependency(LauncherNamespace, GamesNamespace);
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

    [Fact]
    public void Configuration_does_not_depend_on_projects()
    {
        AssertNoDependency(ConfigurationNamespace, ProjectsNamespace);
    }

    [Fact]
    public void Projects_depends_on_configuration_contracts()
    {
        // Projects owns the Program and Axis structural nodes surfaced through the
        // Configuration presentation boundary, so it consumes Configuration's published
        // contracts. Its absence would mean the catalogue-presentation boundary was
        // inverted.
        var dependsOnConfiguration = TypesIn(ProjectsNamespace)
            .SelectMany(ReferencedTypes)
            .Any(referenced => IsInNamespace(referenced, ConfigurationNamespace));

        Assert.True(
            dependsOnConfiguration,
            "Projects must depend on Configuration's published catalog contracts.");
    }

    [Fact]
    public void Projects_does_not_depend_on_other_business_modules()
    {
        // Projects is an independent business module: it may consume Configuration,
        // Attachments, Identity, and platform contracts (and contributes a constant
        // non-attention launcher state), but it must remain independent from every
        // other business module.
        AssertNoDependency(ProjectsNamespace, CapexNamespace);
        AssertNoDependency(ProjectsNamespace, OpexNamespace);
        AssertNoDependency(ProjectsNamespace, InventoryNamespace);
        AssertNoDependency(ProjectsNamespace, TravelNamespace);
        AssertNoDependency(ProjectsNamespace, ClothesNamespace);
        AssertNoDependency(ProjectsNamespace, AssetsNamespace);
        AssertNoDependency(ProjectsNamespace, MoodNamespace);
        AssertNoDependency(ProjectsNamespace, MaintenanceNamespace);
    }

    [Fact]
    public void No_module_depends_on_projects()
    {
        // Projects publishes no cross-module contracts: no other module, including
        // Configuration and Launcher, may reference the Projects namespace.
        AssertNoDependency(ConfigurationNamespace, ProjectsNamespace);
        AssertNoDependency(LauncherNamespace, ProjectsNamespace);
        AssertNoDependency(CapexNamespace, ProjectsNamespace);
        AssertNoDependency(OpexNamespace, ProjectsNamespace);
        AssertNoDependency(InventoryNamespace, ProjectsNamespace);
        AssertNoDependency(TravelNamespace, ProjectsNamespace);
        AssertNoDependency(ClothesNamespace, ProjectsNamespace);
        AssertNoDependency(AssetsNamespace, ProjectsNamespace);
        AssertNoDependency(MoodNamespace, ProjectsNamespace);
        AssertNoDependency(MaintenanceNamespace, ProjectsNamespace);
    }

    [Fact]
    public void Configuration_does_not_depend_on_processes()
    {
        AssertNoDependency(ConfigurationNamespace, ProcessesNamespace);
    }

    [Fact]
    public void Processes_depends_on_configuration_contracts()
    {
        // Processes owns the ProcessCategory catalogue surfaced through the Configuration
        // presentation boundary, so it consumes Configuration's published contracts. Its
        // absence would mean the catalogue-presentation boundary was inverted.
        var dependsOnConfiguration = TypesIn(ProcessesNamespace)
            .SelectMany(ReferencedTypes)
            .Any(referenced => IsInNamespace(referenced, ConfigurationNamespace));

        Assert.True(
            dependsOnConfiguration,
            "Processes must depend on Configuration's published catalog contracts.");
    }

    [Fact]
    public void Processes_does_not_depend_on_other_business_modules()
    {
        // Processes is an independent business module: it may consume Configuration,
        // Attachments, Identity, and platform contracts (and contributes a launcher
        // attention state), but it must remain independent from every other business
        // module, including Projects.
        AssertNoDependency(ProcessesNamespace, CapexNamespace);
        AssertNoDependency(ProcessesNamespace, OpexNamespace);
        AssertNoDependency(ProcessesNamespace, InventoryNamespace);
        AssertNoDependency(ProcessesNamespace, TravelNamespace);
        AssertNoDependency(ProcessesNamespace, ClothesNamespace);
        AssertNoDependency(ProcessesNamespace, AssetsNamespace);
        AssertNoDependency(ProcessesNamespace, MoodNamespace);
        AssertNoDependency(ProcessesNamespace, MaintenanceNamespace);
        AssertNoDependency(ProcessesNamespace, ProjectsNamespace);
    }

    [Fact]
    public void No_module_depends_on_processes()
    {
        // Processes publishes no cross-module contracts: no other module, including
        // Configuration and Launcher, may reference the Processes namespace.
        AssertNoDependency(ConfigurationNamespace, ProcessesNamespace);
        AssertNoDependency(LauncherNamespace, ProcessesNamespace);
        AssertNoDependency(CapexNamespace, ProcessesNamespace);
        AssertNoDependency(OpexNamespace, ProcessesNamespace);
        AssertNoDependency(InventoryNamespace, ProcessesNamespace);
        AssertNoDependency(TravelNamespace, ProcessesNamespace);
        AssertNoDependency(ClothesNamespace, ProcessesNamespace);
        AssertNoDependency(AssetsNamespace, ProcessesNamespace);
        AssertNoDependency(MoodNamespace, ProcessesNamespace);
        AssertNoDependency(MaintenanceNamespace, ProcessesNamespace);
        AssertNoDependency(ProjectsNamespace, ProcessesNamespace);
    }

    [Fact]
    public void Configuration_does_not_depend_on_firebird()
    {
        AssertNoDependency(ConfigurationNamespace, FirebirdNamespace);
    }

    [Fact]
    public void Firebird_depends_on_configuration_contracts()
    {
        // Firebird owns PersonCategory and UsernamePlatform catalogues surfaced
        // through Configuration, so it consumes Configuration's published catalog
        // contracts. Its absence would mean the catalogue-presentation boundary was
        // inverted.
        var dependsOnConfiguration = TypesIn(FirebirdNamespace)
            .SelectMany(ReferencedTypes)
            .Any(referenced => IsInNamespace(referenced, ConfigurationNamespace));

        Assert.True(
            dependsOnConfiguration,
            "Firebird must depend on Configuration's published catalog contracts.");
    }

    [Fact]
    public void Firebird_does_not_depend_on_other_business_modules()
    {
        // Firebird is an independent business module: it may consume Configuration,
        // Attachments, Identity, Launcher, and platform contracts, but it must remain
        // independent from every other business module.
        AssertNoDependency(FirebirdNamespace, CapexNamespace);
        AssertNoDependency(FirebirdNamespace, OpexNamespace);
        AssertNoDependency(FirebirdNamespace, InventoryNamespace);
        AssertNoDependency(FirebirdNamespace, TravelNamespace);
        AssertNoDependency(FirebirdNamespace, ClothesNamespace);
        AssertNoDependency(FirebirdNamespace, AssetsNamespace);
        AssertNoDependency(FirebirdNamespace, MoodNamespace);
        AssertNoDependency(FirebirdNamespace, MaintenanceNamespace);
        AssertNoDependency(FirebirdNamespace, ProjectsNamespace);
        AssertNoDependency(FirebirdNamespace, ProcessesNamespace);
    }

    [Fact]
    public void No_module_depends_on_firebird()
    {
        // Firebird publishes no cross-module business contracts: no other module,
        // including Configuration and Launcher, may reference the Firebird namespace.
        AssertNoDependency(ConfigurationNamespace, FirebirdNamespace);
        AssertNoDependency(LauncherNamespace, FirebirdNamespace);
        AssertNoDependency(CapexNamespace, FirebirdNamespace);
        AssertNoDependency(OpexNamespace, FirebirdNamespace);
        AssertNoDependency(InventoryNamespace, FirebirdNamespace);
        AssertNoDependency(TravelNamespace, FirebirdNamespace);
        AssertNoDependency(ClothesNamespace, FirebirdNamespace);
        AssertNoDependency(AssetsNamespace, FirebirdNamespace);
        AssertNoDependency(MoodNamespace, FirebirdNamespace);
        AssertNoDependency(MaintenanceNamespace, FirebirdNamespace);
        AssertNoDependency(ProjectsNamespace, FirebirdNamespace);
        AssertNoDependency(ProcessesNamespace, FirebirdNamespace);
    }

    [Fact]
    public void Configuration_does_not_depend_on_recipes()
    {
        AssertNoDependency(ConfigurationNamespace, RecipesNamespace);
    }

    [Fact]
    public void Recipes_depends_on_configuration_contracts()
    {
        // Recipes owns the RecipeCategory catalogue surfaced through the Configuration
        // presentation boundary, so it consumes Configuration's published contracts. Its
        // absence would mean the catalogue-presentation boundary was inverted.
        var dependsOnConfiguration = TypesIn(RecipesNamespace)
            .SelectMany(ReferencedTypes)
            .Any(referenced => IsInNamespace(referenced, ConfigurationNamespace));

        Assert.True(
            dependsOnConfiguration,
            "Recipes must depend on Configuration's published catalog contracts.");
    }

    [Fact]
    public void Recipes_does_not_depend_on_other_business_modules()
    {
        // Recipes is the second business module that references another business module:
        // it may consume Inventory (the item read contract and the deletion-reference
        // contract) and Configuration, but it must remain independent from every other
        // business module. It contributes no launcher attention.
        AssertNoDependency(RecipesNamespace, CapexNamespace);
        AssertNoDependency(RecipesNamespace, OpexNamespace);
        AssertNoDependency(RecipesNamespace, TravelNamespace);
        AssertNoDependency(RecipesNamespace, ClothesNamespace);
        AssertNoDependency(RecipesNamespace, AssetsNamespace);
        AssertNoDependency(RecipesNamespace, MoodNamespace);
        AssertNoDependency(RecipesNamespace, MaintenanceNamespace);
        AssertNoDependency(RecipesNamespace, ProjectsNamespace);
        AssertNoDependency(RecipesNamespace, ProcessesNamespace);
        AssertNoDependency(RecipesNamespace, FirebirdNamespace);
        AssertNoDependency(RecipesNamespace, DestinationsNamespace);
    }

    [Fact]
    public void Recipes_does_not_depend_on_launcher()
    {
        // Recipes contributes no launcher attention, so it must not reference the
        // Launcher namespace at all.
        AssertNoDependency(RecipesNamespace, LauncherNamespace);
    }

    [Fact]
    public void Recipes_may_only_consume_inventory_through_published_contracts()
    {
        // The Recipes -> Inventory seam is the second business-to-business dependency,
        // so it is policed like the Maintenance -> Assets seam: Recipes may reference
        // only the Inventory.Contracts namespace (the read contract and the
        // deletion-reference contract) and never Inventory domain, persistence,
        // queries, or mutations.
        var violations = TypesIn(RecipesNamespace)
            .SelectMany(type => ReferencedTypes(type)
                .Where(referenced => IsInNamespace(referenced, InventoryNamespace)
                    && !IsInNamespace(referenced, InventoryContractsNamespace))
                .Select(referenced => $"{type.FullName} -> {referenced.FullName}"))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Recipes may consume Inventory only through 'Inventory.Contracts':"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Inventory_does_not_depend_on_recipes()
    {
        // The item-deletion link clearing is implemented by contract inversion: Inventory
        // defines the read and deletion-reference contracts, Recipes implements them, and
        // Inventory enumerates registered handlers without ever referencing Recipes. This
        // keeps the dependency direction Recipes -> Inventory.
        AssertNoDependency(InventoryNamespace, RecipesNamespace);
    }

    [Fact]
    public void Inventory_publishes_the_cross_module_reference_contracts()
    {
        // The read contract and the deletion-reference contract are the cross-module
        // seam. Keeping them in the Inventory namespace is what preserves the
        // Recipes -> Inventory direction when Recipes consumes and implements them.
        var reader = ApiAssembly.GetType(
            "Segaris.Api.Modules.Inventory.Contracts.IInventoryItemReferenceReader",
            throwOnError: false);
        var deletionHandler = ApiAssembly.GetType(
            "Segaris.Api.Modules.Inventory.Contracts.IInventoryItemDeletionReferenceHandler",
            throwOnError: false);

        Assert.NotNull(reader);
        Assert.NotNull(deletionHandler);
        Assert.True(IsInNamespace(reader!, InventoryContractsNamespace));
        Assert.True(IsInNamespace(deletionHandler!, InventoryContractsNamespace));
    }

    [Fact]
    public void No_module_depends_on_recipes()
    {
        // Recipes publishes no cross-module contracts: no other module, including
        // Configuration, Inventory, and Launcher, may reference the Recipes namespace.
        AssertNoDependency(ConfigurationNamespace, RecipesNamespace);
        AssertNoDependency(LauncherNamespace, RecipesNamespace);
        AssertNoDependency(CapexNamespace, RecipesNamespace);
        AssertNoDependency(OpexNamespace, RecipesNamespace);
        AssertNoDependency(InventoryNamespace, RecipesNamespace);
        AssertNoDependency(TravelNamespace, RecipesNamespace);
        AssertNoDependency(ClothesNamespace, RecipesNamespace);
        AssertNoDependency(AssetsNamespace, RecipesNamespace);
        AssertNoDependency(MoodNamespace, RecipesNamespace);
        AssertNoDependency(MaintenanceNamespace, RecipesNamespace);
        AssertNoDependency(ProjectsNamespace, RecipesNamespace);
        AssertNoDependency(ProcessesNamespace, RecipesNamespace);
        AssertNoDependency(FirebirdNamespace, RecipesNamespace);
        AssertNoDependency(DestinationsNamespace, RecipesNamespace);
    }

    [Fact]
    public void Configuration_does_not_depend_on_destinations()
    {
        AssertNoDependency(ConfigurationNamespace, DestinationsNamespace);
    }

    [Fact]
    public void Destinations_depends_on_configuration_contracts()
    {
        // Destinations owns DestinationCategory and PlaceCategory catalogues surfaced
        // through the Configuration presentation boundary, so it consumes
        // Configuration's published contracts. Its absence would mean the
        // catalogue-presentation boundary was inverted.
        var dependsOnConfiguration = TypesIn(DestinationsNamespace)
            .SelectMany(ReferencedTypes)
            .Any(referenced => IsInNamespace(referenced, ConfigurationNamespace));

        Assert.True(
            dependsOnConfiguration,
            "Destinations must depend on Configuration's published catalog contracts.");
    }

    [Fact]
    public void Destinations_does_not_depend_on_other_business_modules()
    {
        // Destinations is an independent business module that publishes the
        // destination reference seam consumed by Travel. It may consume
        // Configuration and platform contracts, but it must not depend on any other
        // business module and contributes no launcher attention.
        AssertNoDependency(DestinationsNamespace, CapexNamespace);
        AssertNoDependency(DestinationsNamespace, OpexNamespace);
        AssertNoDependency(DestinationsNamespace, InventoryNamespace);
        AssertNoDependency(DestinationsNamespace, TravelNamespace);
        AssertNoDependency(DestinationsNamespace, ClothesNamespace);
        AssertNoDependency(DestinationsNamespace, AssetsNamespace);
        AssertNoDependency(DestinationsNamespace, MoodNamespace);
        AssertNoDependency(DestinationsNamespace, MaintenanceNamespace);
        AssertNoDependency(DestinationsNamespace, ProjectsNamespace);
        AssertNoDependency(DestinationsNamespace, ProcessesNamespace);
        AssertNoDependency(DestinationsNamespace, FirebirdNamespace);
        AssertNoDependency(DestinationsNamespace, RecipesNamespace);
    }

    [Fact]
    public void Destinations_does_not_depend_on_launcher()
    {
        // Destinations contributes no launcher attention, so it must not reference the
        // Launcher namespace at all.
        AssertNoDependency(DestinationsNamespace, LauncherNamespace);
    }

    [Fact]
    public void Travel_may_only_consume_destinations_through_published_contracts()
    {
        // The Travel -> Destinations seam is policed like the other business-to-business
        // dependencies: Travel may reference only Destinations.Contracts and never
        // Destinations domain, persistence, queries, or mutations.
        var violations = TypesIn(TravelNamespace)
            .SelectMany(type => ReferencedTypes(type)
                .Where(referenced => IsInNamespace(referenced, DestinationsNamespace)
                    && !IsInNamespace(referenced, DestinationsContractsNamespace))
                .Select(referenced => $"{type.FullName} -> {referenced.FullName}"))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Travel may consume Destinations only through 'Destinations.Contracts':"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Destinations_publishes_the_cross_module_reference_contracts()
    {
        // The read contract and deletion-reference contract are the cross-module
        // seam. Keeping them in the Destinations namespace is what preserves the
        // Travel -> Destinations direction when Travel consumes and implements them.
        var reader = ApiAssembly.GetType(
            "Segaris.Api.Modules.Destinations.Contracts.IDestinationReferenceReader",
            throwOnError: false);
        var deletionHandler = ApiAssembly.GetType(
            "Segaris.Api.Modules.Destinations.Contracts.IDestinationDeletionReferenceHandler",
            throwOnError: false);

        Assert.NotNull(reader);
        Assert.NotNull(deletionHandler);
        Assert.True(IsInNamespace(reader!, DestinationsContractsNamespace));
        Assert.True(IsInNamespace(deletionHandler!, DestinationsContractsNamespace));
    }

    [Fact]
    public void Only_travel_may_depend_on_destinations()
    {
        // Destinations publishes a cross-module reference seam only for Travel. Other
        // modules, including Configuration and Launcher, may not reference
        // Destinations directly.
        AssertNoDependency(ConfigurationNamespace, DestinationsNamespace);
        AssertNoDependency(LauncherNamespace, DestinationsNamespace);
        AssertNoDependency(CapexNamespace, DestinationsNamespace);
        AssertNoDependency(OpexNamespace, DestinationsNamespace);
        AssertNoDependency(InventoryNamespace, DestinationsNamespace);
        AssertNoDependency(ClothesNamespace, DestinationsNamespace);
        AssertNoDependency(AssetsNamespace, DestinationsNamespace);
        AssertNoDependency(MoodNamespace, DestinationsNamespace);
        AssertNoDependency(MaintenanceNamespace, DestinationsNamespace);
        AssertNoDependency(ProjectsNamespace, DestinationsNamespace);
        AssertNoDependency(ProcessesNamespace, DestinationsNamespace);
        AssertNoDependency(FirebirdNamespace, DestinationsNamespace);
        AssertNoDependency(RecipesNamespace, DestinationsNamespace);
        AssertNoDependency(HealthNamespace, DestinationsNamespace);
    }

    [Fact]
    public void Configuration_does_not_depend_on_health()
    {
        AssertNoDependency(ConfigurationNamespace, HealthNamespace);
    }

    [Fact]
    public void Health_depends_on_configuration_contracts()
    {
        var dependsOnConfiguration = TypesIn(HealthNamespace)
            .SelectMany(ReferencedTypes)
            .Any(referenced => IsInNamespace(referenced, ConfigurationNamespace));

        Assert.True(
            dependsOnConfiguration,
            "Health must depend on Configuration's published catalog contracts.");
    }

    [Fact]
    public void Health_does_not_depend_on_forbidden_business_modules()
    {
        // Health may consume Configuration, Inventory.Contracts, and platform
        // contracts. It must remain independent from all other business modules and
        // contributes no launcher attention.
        AssertNoDependency(HealthNamespace, CapexNamespace);
        AssertNoDependency(HealthNamespace, OpexNamespace);
        AssertNoDependency(HealthNamespace, TravelNamespace);
        AssertNoDependency(HealthNamespace, ClothesNamespace);
        AssertNoDependency(HealthNamespace, AssetsNamespace);
        AssertNoDependency(HealthNamespace, MoodNamespace);
        AssertNoDependency(HealthNamespace, MaintenanceNamespace);
        AssertNoDependency(HealthNamespace, ProjectsNamespace);
        AssertNoDependency(HealthNamespace, ProcessesNamespace);
        AssertNoDependency(HealthNamespace, FirebirdNamespace);
        AssertNoDependency(HealthNamespace, RecipesNamespace);
        AssertNoDependency(HealthNamespace, DestinationsNamespace);
        AssertNoDependency(HealthNamespace, LauncherNamespace);
    }

    [Fact]
    public void Health_may_only_consume_inventory_through_published_contracts()
    {
        var violations = TypesIn(HealthNamespace)
            .SelectMany(type => ReferencedTypes(type)
                .Where(referenced => IsInNamespace(referenced, InventoryNamespace)
                    && !IsInNamespace(referenced, InventoryContractsNamespace))
                .Select(referenced => $"{type.FullName} -> {referenced.FullName}"))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Health may consume Inventory only through 'Inventory.Contracts':"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Inventory_does_not_depend_on_health()
    {
        AssertNoDependency(InventoryNamespace, HealthNamespace);
    }

    [Fact]
    public void Health_publishes_no_cross_module_business_contracts()
    {
        // Health's contracts are currently API/configuration/inventory-consumption
        // contracts for its own implementation. No sibling business module may
        // reference Health in Wave 0.
        Assert.NotEmpty(TypesIn(HealthContractsNamespace));
        AssertNoDependency(ConfigurationNamespace, HealthNamespace);
        AssertNoDependency(LauncherNamespace, HealthNamespace);
        AssertNoDependency(CapexNamespace, HealthNamespace);
        AssertNoDependency(OpexNamespace, HealthNamespace);
        AssertNoDependency(InventoryNamespace, HealthNamespace);
        AssertNoDependency(TravelNamespace, HealthNamespace);
        AssertNoDependency(ClothesNamespace, HealthNamespace);
        AssertNoDependency(AssetsNamespace, HealthNamespace);
        AssertNoDependency(MoodNamespace, HealthNamespace);
        AssertNoDependency(MaintenanceNamespace, HealthNamespace);
        AssertNoDependency(ProjectsNamespace, HealthNamespace);
        AssertNoDependency(ProcessesNamespace, HealthNamespace);
        AssertNoDependency(FirebirdNamespace, HealthNamespace);
        AssertNoDependency(RecipesNamespace, HealthNamespace);
        AssertNoDependency(DestinationsNamespace, HealthNamespace);
    }

    [Fact]
    public void Configuration_does_not_depend_on_games()
    {
        AssertNoDependency(ConfigurationNamespace, GamesNamespace);
    }

    [Fact]
    public void Games_depends_on_configuration_contracts()
    {
        // Games owns the Game catalogue surfaced through the Configuration
        // presentation boundary, so it consumes Configuration's published contracts.
        // Its absence would mean the catalogue-presentation boundary was inverted.
        var dependsOnConfiguration = TypesIn(GamesNamespace)
            .SelectMany(ReferencedTypes)
            .Any(referenced => IsInNamespace(referenced, ConfigurationNamespace));

        Assert.True(
            dependsOnConfiguration,
            "Games must depend on Configuration's published catalog contracts.");
    }

    [Fact]
    public void Games_does_not_depend_on_forbidden_business_modules()
    {
        // Games is an independent business module: it may consume Configuration,
        // Launcher, Identity, and platform contracts (its launcher card reports a
        // constant non-attention state from Wave 1 on), but it must remain
        // independent from every other business module.
        AssertNoDependency(GamesNamespace, CapexNamespace);
        AssertNoDependency(GamesNamespace, OpexNamespace);
        AssertNoDependency(GamesNamespace, InventoryNamespace);
        AssertNoDependency(GamesNamespace, TravelNamespace);
        AssertNoDependency(GamesNamespace, ClothesNamespace);
        AssertNoDependency(GamesNamespace, AssetsNamespace);
        AssertNoDependency(GamesNamespace, MoodNamespace);
        AssertNoDependency(GamesNamespace, MaintenanceNamespace);
        AssertNoDependency(GamesNamespace, ProjectsNamespace);
        AssertNoDependency(GamesNamespace, ProcessesNamespace);
        AssertNoDependency(GamesNamespace, FirebirdNamespace);
        AssertNoDependency(GamesNamespace, RecipesNamespace);
        AssertNoDependency(GamesNamespace, DestinationsNamespace);
        AssertNoDependency(GamesNamespace, HealthNamespace);
        AssertNoDependency(GamesNamespace, CalendarNamespace);
        AssertNoDependency(GamesNamespace, AnalyticsNamespace);
    }

    [Fact]
    public void No_module_depends_on_games()
    {
        // Games publishes no cross-module business contracts: no other module,
        // including Configuration and Launcher, may reference the Games namespace.
        Assert.NotEmpty(TypesIn(GamesContractsNamespace));
        AssertNoDependency(ConfigurationNamespace, GamesNamespace);
        AssertNoDependency(LauncherNamespace, GamesNamespace);
        AssertNoDependency(CapexNamespace, GamesNamespace);
        AssertNoDependency(OpexNamespace, GamesNamespace);
        AssertNoDependency(InventoryNamespace, GamesNamespace);
        AssertNoDependency(TravelNamespace, GamesNamespace);
        AssertNoDependency(ClothesNamespace, GamesNamespace);
        AssertNoDependency(AssetsNamespace, GamesNamespace);
        AssertNoDependency(MoodNamespace, GamesNamespace);
        AssertNoDependency(MaintenanceNamespace, GamesNamespace);
        AssertNoDependency(ProjectsNamespace, GamesNamespace);
        AssertNoDependency(ProcessesNamespace, GamesNamespace);
        AssertNoDependency(FirebirdNamespace, GamesNamespace);
        AssertNoDependency(RecipesNamespace, GamesNamespace);
        AssertNoDependency(DestinationsNamespace, GamesNamespace);
        AssertNoDependency(HealthNamespace, GamesNamespace);
        AssertNoDependency(CalendarNamespace, GamesNamespace);
        AssertNoDependency(AnalyticsNamespace, GamesNamespace);
    }

    [Fact]
    public void Calendar_consumes_only_initial_source_projection_contracts()
    {
        AssertCalendarReferencesContract(FirebirdContractsNamespace);
        AssertCalendarReferencesContract(TravelContractsNamespace);
        AssertCalendarReferencesContract(InventoryContractsNamespace);
        AssertCalendarReferencesContract(AssetsContractsNamespace);
        AssertCalendarReferencesContract(MaintenanceContractsNamespace);
        AssertCalendarReferencesContract(ProcessesContractsNamespace);

        AssertMayOnlyReferenceNamespaceThroughContracts(CalendarNamespace, FirebirdNamespace, FirebirdContractsNamespace);
        AssertMayOnlyReferenceNamespaceThroughContracts(CalendarNamespace, TravelNamespace, TravelContractsNamespace);
        AssertMayOnlyReferenceNamespaceThroughContracts(CalendarNamespace, InventoryNamespace, InventoryContractsNamespace);
        AssertMayOnlyReferenceNamespaceThroughContracts(CalendarNamespace, AssetsNamespace, AssetsContractsNamespace);
        AssertMayOnlyReferenceNamespaceThroughContracts(CalendarNamespace, MaintenanceNamespace, MaintenanceContractsNamespace);
        AssertMayOnlyReferenceNamespaceThroughContracts(CalendarNamespace, ProcessesNamespace, ProcessesContractsNamespace);

        AssertNoDependency(CalendarNamespace, CapexNamespace);
        AssertNoDependency(CalendarNamespace, OpexNamespace);
        AssertNoDependency(CalendarNamespace, ClothesNamespace);
        AssertNoDependency(CalendarNamespace, MoodNamespace);
        AssertNoDependency(CalendarNamespace, ProjectsNamespace);
        AssertNoDependency(CalendarNamespace, RecipesNamespace);
        AssertNoDependency(CalendarNamespace, DestinationsNamespace);
        AssertNoDependency(CalendarNamespace, HealthNamespace);
        AssertNoDependency(CalendarNamespace, LauncherNamespace);
    }

    [Fact]
    public void Source_modules_do_not_depend_on_calendar()
    {
        AssertNoDependency(FirebirdNamespace, CalendarNamespace);
        AssertNoDependency(TravelNamespace, CalendarNamespace);
        AssertNoDependency(InventoryNamespace, CalendarNamespace);
        AssertNoDependency(AssetsNamespace, CalendarNamespace);
        AssertNoDependency(MaintenanceNamespace, CalendarNamespace);
        AssertNoDependency(ProcessesNamespace, CalendarNamespace);
        AssertNoDependency(CapexNamespace, CalendarNamespace);
        AssertNoDependency(OpexNamespace, CalendarNamespace);
        AssertNoDependency(RecipesNamespace, CalendarNamespace);
        AssertNoDependency(DestinationsNamespace, CalendarNamespace);
        AssertNoDependency(HealthNamespace, CalendarNamespace);
        AssertNoDependency(LauncherNamespace, CalendarNamespace);
    }

    [Fact]
    public void Analytics_consumes_only_initial_source_projection_contracts()
    {
        AssertAnalyticsReferencesContract(CapexContractsNamespace);
        AssertAnalyticsReferencesContract(OpexContractsNamespace);
        AssertAnalyticsReferencesContract(InventoryContractsNamespace);
        AssertAnalyticsReferencesContract(TravelContractsNamespace);
        AssertAnalyticsReferencesContract(ConfigurationContractsNamespace);

        AssertMayOnlyReferenceNamespaceThroughContracts(AnalyticsNamespace, CapexNamespace, CapexContractsNamespace);
        AssertMayOnlyReferenceNamespaceThroughContracts(AnalyticsNamespace, OpexNamespace, OpexContractsNamespace);
        AssertMayOnlyReferenceNamespaceThroughContracts(AnalyticsNamespace, InventoryNamespace, InventoryContractsNamespace);
        AssertMayOnlyReferenceNamespaceThroughContracts(AnalyticsNamespace, TravelNamespace, TravelContractsNamespace);
        AssertMayOnlyReferenceNamespaceThroughContracts(AnalyticsNamespace, ConfigurationNamespace, ConfigurationContractsNamespace);

        AssertNoDependency(AnalyticsNamespace, CalendarNamespace);
        AssertNoDependency(AnalyticsNamespace, ClothesNamespace);
        AssertNoDependency(AnalyticsNamespace, AssetsNamespace);
        AssertNoDependency(AnalyticsNamespace, MoodNamespace);
        AssertNoDependency(AnalyticsNamespace, MaintenanceNamespace);
        AssertNoDependency(AnalyticsNamespace, ProjectsNamespace);
        AssertNoDependency(AnalyticsNamespace, ProcessesNamespace);
        AssertNoDependency(AnalyticsNamespace, FirebirdNamespace);
        AssertNoDependency(AnalyticsNamespace, RecipesNamespace);
        AssertNoDependency(AnalyticsNamespace, DestinationsNamespace);
        AssertNoDependency(AnalyticsNamespace, HealthNamespace);
        AssertNoDependency(AnalyticsNamespace, LauncherNamespace);
    }

    [Fact]
    public void Source_modules_do_not_depend_on_analytics()
    {
        AssertNoDependency(ConfigurationNamespace, AnalyticsNamespace);
        AssertNoDependency(CapexNamespace, AnalyticsNamespace);
        AssertNoDependency(OpexNamespace, AnalyticsNamespace);
        AssertNoDependency(InventoryNamespace, AnalyticsNamespace);
        AssertNoDependency(TravelNamespace, AnalyticsNamespace);
        AssertNoDependency(CalendarNamespace, AnalyticsNamespace);
        AssertNoDependency(ClothesNamespace, AnalyticsNamespace);
        AssertNoDependency(AssetsNamespace, AnalyticsNamespace);
        AssertNoDependency(MoodNamespace, AnalyticsNamespace);
        AssertNoDependency(MaintenanceNamespace, AnalyticsNamespace);
        AssertNoDependency(ProjectsNamespace, AnalyticsNamespace);
        AssertNoDependency(ProcessesNamespace, AnalyticsNamespace);
        AssertNoDependency(FirebirdNamespace, AnalyticsNamespace);
        AssertNoDependency(RecipesNamespace, AnalyticsNamespace);
        AssertNoDependency(DestinationsNamespace, AnalyticsNamespace);
        AssertNoDependency(HealthNamespace, AnalyticsNamespace);
        AssertNoDependency(LauncherNamespace, AnalyticsNamespace);
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

    private static void AssertMayOnlyReferenceNamespaceThroughContracts(
        string sourceNamespace,
        string dependencyNamespace,
        string contractsNamespace)
    {
        var violations = TypesIn(sourceNamespace)
            .SelectMany(type => ReferencedTypes(type)
                .Where(referenced => IsInNamespace(referenced, dependencyNamespace)
                    && !IsInNamespace(referenced, contractsNamespace))
                .Select(referenced => $"{type.FullName} -> {referenced.FullName}"))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Types in '{sourceNamespace}' may consume '{dependencyNamespace}' only through '{contractsNamespace}':"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    private static void AssertCalendarReferencesContract(string contractsNamespace)
    {
        var referencesContract = TypesIn(CalendarNamespace)
            .SelectMany(ReferencedTypes)
            .Any(referenced => IsInNamespace(referenced, contractsNamespace));

        Assert.True(
            referencesContract,
            $"Calendar must consume a published projection contract from '{contractsNamespace}'.");
    }

    private static void AssertAnalyticsReferencesContract(string contractsNamespace)
    {
        var referencesContract = TypesIn(AnalyticsNamespace)
            .SelectMany(ReferencedTypes)
            .Any(referenced => IsInNamespace(referenced, contractsNamespace));

        Assert.True(
            referencesContract,
            $"Analytics must consume a published contract from '{contractsNamespace}'.");
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
