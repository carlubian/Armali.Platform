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

    /// <summary>
    /// Acceptance criterion 12: <c>/health/live</c> stays at 200 with PostgreSQL down.
    /// </summary>
    /// <remarks>
    /// This test used to start a host against port 1, so the database was unreachable from the
    /// very first request. Phase 2 made the host apply migrations at startup, and a host that
    /// starts against an unreachable database now fails to start at all: it never gets as far as
    /// having a pipeline to probe, so the old arrangement could no longer demonstrate anything.
    /// The scenario is therefore staged the other way round. The host starts healthy against a
    /// real database of its own and the database is taken away underneath it, which is also the
    /// closer match to what the criterion guards against: PostgreSQL falling over while the
    /// backend is running, not a backend deployed without one.
    /// </remarks>
    [Fact]
    public async Task Liveness_reports_healthy_even_when_the_database_becomes_unreachable()
    {
        ArgumentNullException.ThrowIfNull(postgres);

        var connectionString = await postgres.CreateDatabaseAsync(CancellationToken.None);
        using var factory = new BlackwingApiFactory(connectionString);
        using var client = factory.CreateClient();

        var readinessWhileHealthy = await client.GetAsync("/health/ready", CancellationToken.None);

        await postgres.DropDatabaseAsync(connectionString, CancellationToken.None);

        var liveness = await client.GetAsync("/health/live", CancellationToken.None);
        var readiness = await client.GetAsync("/health/ready", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, readinessWhileHealthy.StatusCode);

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
