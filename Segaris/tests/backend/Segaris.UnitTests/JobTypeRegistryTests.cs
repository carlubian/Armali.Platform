using Segaris.Api.Platform.Jobs;

namespace Segaris.UnitTests;

public sealed class JobTypeRegistryTests
{
    [Fact]
    public void Resolves_registered_job_types()
    {
        var registry = new JobTypeRegistry(
        [
            new JobTypeRegistration("backup", "backup", typeof(object)),
            new JobTypeRegistration("report", null, typeof(string)),
        ]);

        Assert.Equal("backup", registry.Get("backup").ExclusivityKey);
        Assert.Null(registry.Get("report").ExclusivityKey);
        Assert.True(registry.TryGet("backup", out _));
        Assert.False(registry.TryGet("unknown", out _));
    }

    [Fact]
    public void Rejects_duplicate_job_types()
    {
        Assert.Throws<InvalidOperationException>(() => new JobTypeRegistry(
        [
            new JobTypeRegistration("backup", "backup", typeof(object)),
            new JobTypeRegistration("backup", "backup", typeof(string)),
        ]));
    }

    [Fact]
    public void Get_throws_for_unknown_job_type()
    {
        var registry = new JobTypeRegistry([]);
        Assert.Throws<InvalidOperationException>(() => registry.Get("missing"));
    }
}
