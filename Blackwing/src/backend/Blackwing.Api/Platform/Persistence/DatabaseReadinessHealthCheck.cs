using Blackwing.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Blackwing.Api.Platform.Persistence;

/// <summary>
/// Readiness probe for PostgreSQL. It only checks reachability: the schema and its migrations
/// arrive in a later phase.
/// </summary>
internal sealed class DatabaseReadinessHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<BlackwingDbContext>();
            if (!await database.Database.CanConnectAsync(cancellationToken))
            {
                return HealthCheckResult.Unhealthy("The Blackwing database is unavailable.");
            }

            return HealthCheckResult.Healthy("The Blackwing database is reachable.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("The Blackwing database readiness check failed.", exception);
        }
    }
}
