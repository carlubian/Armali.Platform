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
}
