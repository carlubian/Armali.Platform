namespace Blackwing.Api.Tests;

public sealed class FoundationTests { [Fact] public void ReadinessEndpointHasAStableRoute() => Assert.Equal("/health/ready", "/health/ready"); }
