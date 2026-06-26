using Segaris.Api.Composition;

namespace Segaris.UnitTests;

public sealed class ModuleRegistrationTests
{
    [Fact]
    public void Registered_module_names_are_unique()
    {
        var names = SegarisModules.ModuleNames;

        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Capex_modules_are_registered()
    {
        var names = SegarisModules.ModuleNames;

        Assert.Contains("Configuration", names);
        Assert.Contains("Capex", names);
        Assert.Contains("Opex", names);
        Assert.Contains("Inventory", names);
        Assert.Contains("Travel", names);
        Assert.Contains("Clothes", names);
        Assert.Contains("Assets", names);
        Assert.Contains("Mood", names);
        Assert.Contains("Maintenance", names);
        Assert.Contains("Projects", names);
        Assert.Contains("Processes", names);
        Assert.Contains("Firebird", names);
        Assert.Contains("Recipes", names);
        Assert.Contains("Destinations", names);
        Assert.Contains("Health", names);
        Assert.Contains("Calendar", names);
        Assert.Contains("Analytics", names);
        Assert.Contains("Launcher", names);
    }

    [Fact]
    public void Configuration_is_registered_before_capex_that_consumes_it()
    {
        var names = SegarisModules.ModuleNames.ToList();

        Assert.True(
            names.IndexOf("Configuration") < names.IndexOf("Capex"),
            "Configuration must be registered before Capex.");
    }

    [Fact]
    public void Opex_is_registered_after_configuration()
    {
        var names = SegarisModules.ModuleNames.ToList();

        Assert.True(
            names.IndexOf("Configuration") < names.IndexOf("Opex"),
            "Configuration must be registered before Opex.");
    }

    [Fact]
    public void Inventory_is_registered_after_configuration()
    {
        var names = SegarisModules.ModuleNames.ToList();

        Assert.True(
            names.IndexOf("Configuration") < names.IndexOf("Inventory"),
            "Configuration must be registered before Inventory.");
    }

    [Fact]
    public void Travel_is_registered_after_inventory_and_before_launcher()
    {
        var names = SegarisModules.ModuleNames.ToList();

        Assert.True(
            names.IndexOf("Inventory") < names.IndexOf("Travel"),
            "Travel must be registered after Inventory.");
        Assert.True(
            names.IndexOf("Travel") < names.IndexOf("Launcher"),
            "Travel must be registered before Launcher.");
    }

    [Fact]
    public void Clothes_is_registered_after_travel_and_before_assets()
    {
        var names = SegarisModules.ModuleNames.ToList();

        Assert.True(
            names.IndexOf("Travel") < names.IndexOf("Clothes"),
            "Clothes must be registered after Travel.");
        Assert.True(
            names.IndexOf("Clothes") < names.IndexOf("Assets"),
            "Clothes must be registered before Assets.");
    }

    [Fact]
    public void Assets_is_registered_after_clothes_and_before_mood()
    {
        var names = SegarisModules.ModuleNames.ToList();

        Assert.True(
            names.IndexOf("Clothes") < names.IndexOf("Assets"),
            "Assets must be registered after Clothes.");
        Assert.True(
            names.IndexOf("Assets") < names.IndexOf("Mood"),
            "Assets must be registered before Mood.");
    }

    [Fact]
    public void Mood_is_registered_after_assets_and_before_launcher()
    {
        var names = SegarisModules.ModuleNames.ToList();

        Assert.True(
            names.IndexOf("Assets") < names.IndexOf("Mood"),
            "Mood must be registered after Assets.");
        Assert.True(
            names.IndexOf("Mood") < names.IndexOf("Launcher"),
            "Mood must be registered before Launcher.");
    }

    [Fact]
    public void Projects_is_registered_after_maintenance_and_before_launcher()
    {
        var names = SegarisModules.ModuleNames.ToList();

        Assert.True(
            names.IndexOf("Maintenance") < names.IndexOf("Projects"),
            "Projects must be registered after Maintenance.");
        Assert.True(
            names.IndexOf("Projects") < names.IndexOf("Launcher"),
            "Projects must be registered before Launcher.");
    }

    [Fact]
    public void Processes_is_registered_after_projects_and_before_launcher()
    {
        var names = SegarisModules.ModuleNames.ToList();

        Assert.True(
            names.IndexOf("Projects") < names.IndexOf("Processes"),
            "Processes must be registered after Projects.");
        Assert.True(
            names.IndexOf("Processes") < names.IndexOf("Launcher"),
            "Processes must be registered before Launcher.");
    }

    [Fact]
    public void Firebird_is_registered_after_processes_and_before_launcher()
    {
        var names = SegarisModules.ModuleNames.ToList();

        Assert.True(
            names.IndexOf("Processes") < names.IndexOf("Firebird"),
            "Firebird must be registered after Processes.");
        Assert.True(
            names.IndexOf("Firebird") < names.IndexOf("Launcher"),
            "Firebird must be registered before Launcher.");
    }

    [Fact]
    public void Recipes_is_registered_after_inventory_and_before_launcher()
    {
        var names = SegarisModules.ModuleNames.ToList();

        Assert.True(
            names.IndexOf("Inventory") < names.IndexOf("Recipes"),
            "Recipes must be registered after Inventory, the module it consumes.");
        Assert.True(
            names.IndexOf("Firebird") < names.IndexOf("Recipes"),
            "Recipes must be registered after Firebird.");
        Assert.True(
            names.IndexOf("Recipes") < names.IndexOf("Launcher"),
            "Recipes must be registered before Launcher.");
    }

    [Fact]
    public void Destinations_is_registered_after_recipes_and_before_launcher()
    {
        var names = SegarisModules.ModuleNames.ToList();

        Assert.True(
            names.IndexOf("Recipes") < names.IndexOf("Destinations"),
            "Destinations must be registered after Recipes.");
        Assert.True(
            names.IndexOf("Destinations") < names.IndexOf("Launcher"),
            "Destinations must be registered before Launcher.");
    }

    [Fact]
    public void Health_is_registered_after_destinations_and_before_launcher()
    {
        var names = SegarisModules.ModuleNames.ToList();

        Assert.True(
            names.IndexOf("Destinations") < names.IndexOf("Health"),
            "Health must be registered after Destinations.");
        Assert.True(
            names.IndexOf("Health") < names.IndexOf("Launcher"),
            "Health must be registered before Launcher.");
    }

    [Fact]
    public void Calendar_is_registered_after_health_and_before_launcher()
    {
        var names = SegarisModules.ModuleNames.ToList();

        Assert.True(
            names.IndexOf("Health") < names.IndexOf("Calendar"),
            "Calendar must be registered after Health.");
        Assert.True(
            names.IndexOf("Calendar") < names.IndexOf("Launcher"),
            "Calendar must be registered before Launcher.");
    }

    [Fact]
    public void Analytics_is_registered_after_calendar_and_before_launcher()
    {
        var names = SegarisModules.ModuleNames.ToList();

        Assert.True(
            names.IndexOf("Calendar") < names.IndexOf("Analytics"),
            "Analytics must be registered after Calendar.");
        Assert.True(
            names.IndexOf("Analytics") < names.IndexOf("Launcher"),
            "Analytics must be registered before Launcher.");
    }
}
