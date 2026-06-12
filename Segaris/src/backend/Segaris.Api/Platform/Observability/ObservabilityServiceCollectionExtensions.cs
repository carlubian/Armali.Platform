using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Segaris.Api.Configuration;
using Segaris.Api.Platform.Api;
using Segaris.Api.Platform.Persistence;

namespace Segaris.Api.Platform.Observability;

internal static class ObservabilityServiceCollectionExtensions
{
    internal const string AuthenticationRateLimitPolicy = "authentication";

    public static IServiceCollection AddSegarisObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var diagnostics = configuration.GetSection(DiagnosticsOptions.SectionName)
            .Get<DiagnosticsOptions>() ?? new DiagnosticsOptions();

        services.AddHealthChecks()
            .AddCheck<DatabaseReadinessHealthCheck>("database", tags: ["ready"]);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                var problemDetails = context.HttpContext.RequestServices
                    .GetRequiredService<IProblemDetailsService>();
                await problemDetails.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests.",
                        Detail = "The request rate limit has been exceeded.",
                    },
                });
            };

            options.AddPolicy(AuthenticationRateLimitPolicy, httpContext =>
                CreateFixedWindowPartition(httpContext, permitLimit: 10));
            options.AddPolicy(FrontendDiagnosticsEndpoints.RateLimitPolicy, httpContext =>
                CreateFixedWindowPartition(
                    httpContext,
                    diagnostics.PermitLimit,
                    TimeSpan.FromSeconds(diagnostics.WindowSeconds)));
        });

        return services;
    }

    private static RateLimitPartition<string> CreateFixedWindowPartition(
        HttpContext context,
        int permitLimit,
        TimeSpan? window = null)
    {
        var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            key,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                QueueLimit = 0,
                Window = window ?? TimeSpan.FromMinutes(1),
                AutoReplenishment = true,
            });
    }
}
