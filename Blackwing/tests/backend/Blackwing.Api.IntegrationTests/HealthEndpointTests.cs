using System.Net;
using Blackwing.Api.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Blackwing.Api.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class HealthEndpointTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Readiness_reports_healthy_against_a_real_postgres_instance()
    {
        ArgumentNullException.ThrowIfNull(postgres);

        using var factory = new BlackwingApiFactory(postgres.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready", CancellationToken.None);

        // The endpoint answers with the aggregate status only. When it is not healthy, ask the
        // health check service which entry failed and why: a bare 503 in a CI log is not
        // actionable.
        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Expected readiness to report OK but got {response.StatusCode}. "
                + await DescribeReadinessAsync(factory));
    }

    [Fact]
    public async Task Liveness_reports_healthy_even_when_the_database_is_unreachable()
    {
        // Port 1 is never a PostgreSQL server, so the database check is guaranteed to fail while
        // the process itself stays perfectly healthy.
        using var factory = new BlackwingApiFactory(
            "Host=127.0.0.1;Port=1;Database=blackwing;Username=blackwing;Password=blackwing;Timeout=1");
        using var client = factory.CreateClient();

        var liveness = await client.GetAsync("/health/live", CancellationToken.None);
        var readiness = await client.GetAsync("/health/ready", CancellationToken.None);

        // Liveness must not depend on anything external: reporting the process dead because
        // PostgreSQL is down would have orchestration restart a container that is working.
        Assert.Equal(HttpStatusCode.OK, liveness.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, readiness.StatusCode);
    }

    private static async Task<string> DescribeReadinessAsync(BlackwingApiFactory factory)
    {
        var healthChecks = factory.Services.GetRequiredService<HealthCheckService>();
        var report = await healthChecks.CheckHealthAsync(
            registration => registration.Tags.Contains("ready", StringComparer.Ordinal),
            CancellationToken.None);

        var entries = report.Entries.Select(entry =>
            $"{entry.Key}={entry.Value.Status} ({entry.Value.Description}"
                + $"{(entry.Value.Exception is null ? string.Empty : $": {entry.Value.Exception.Message}")})");

        return string.Join("; ", entries);
    }
}
