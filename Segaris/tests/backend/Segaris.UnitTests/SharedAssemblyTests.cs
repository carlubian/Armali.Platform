using Segaris.Shared;

namespace Segaris.UnitTests;

public sealed class SharedAssemblyTests
{
    [Fact]
    public void Marker_exposes_the_shared_assembly()
    {
        Assert.Equal("Segaris.Shared", SharedAssembly.Assembly.GetName().Name);
    }
}

