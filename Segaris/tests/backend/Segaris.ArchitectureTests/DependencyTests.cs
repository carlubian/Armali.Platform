using Segaris.Persistence;
using Segaris.Shared;

namespace Segaris.ArchitectureTests;

public sealed class DependencyTests
{
    [Fact]
    public void Shared_does_not_reference_the_api_or_modules()
    {
        var references = SharedAssembly.Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.DoesNotContain("Segaris.Api", references);
    }

    [Fact]
    public void Api_is_the_executable_composition_root()
    {
        var apiAssembly = typeof(Program).Assembly;

        Assert.Equal("Segaris.Api", apiAssembly.GetName().Name);
        Assert.NotNull(apiAssembly.EntryPoint);
    }

    [Fact]
    public void Persistence_does_not_reference_the_api_or_modules()
    {
        var references = PersistenceAssembly.Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.DoesNotContain("Segaris.Api", references);
    }
}
