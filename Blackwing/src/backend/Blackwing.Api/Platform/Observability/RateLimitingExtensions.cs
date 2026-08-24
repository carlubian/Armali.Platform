using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Blackwing.Api.Platform.Observability;

internal static class RateLimitingExtensions
{
    /// <summary>Name of the fixed-window policy guarding the login endpoint.</summary>
    internal const string AuthenticationRateLimitPolicy = "authentication";

    private const int AuthenticationPermitLimit = 10;

    /// <summary>
    /// Registers the login rate limiter and the forwarded-header handling it depends on.
    /// </summary>
    /// <remarks>
    /// The two belong together. Partitioning by client IP is only meaningful if the client IP is
    /// the real one, and behind the Caddy ingress <c>Connection.RemoteIpAddress</c> is always the
    /// address of the Caddy container. Without forwarded headers every caller in the house would
    /// share a single partition and one of them could exhaust everyone's quota.
    /// </remarks>
    public static IServiceCollection AddBlackwingRateLimiting(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            // Only X-Forwarded-For. Blackwing does not rewrite scheme or host from headers.
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;

            // Exactly one proxy sits in front of the backend: Caddy. Accepting a longer chain
            // would let a client prepend entries and choose which address is taken as its own.
            options.ForwardLimit = 1;

            // The defaults trust localhost only, which is never the Compose case; an empty pair
            // of collections, on the other hand, trusts everyone and turns the limiter into
            // decoration, because then the client picks its own partition. Both are wrong, so
            // the lists are cleared and repopulated with the networks a proxy can legitimately
            // reach us from: the Docker bridge pool that Compose allocates its internal network
            // from, plus loopback for local runs and in-process test hosts.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("172.16.0.0"), 12));
            options.KnownProxies.Add(IPAddress.Loopback);
            options.KnownProxies.Add(IPAddress.IPv6Loopback);
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Without this the rejected request gets an empty body, which the frontend cannot
            // tell apart from a network failure. Every refusal answers in problem+json.
            options.OnRejected = async (context, cancellationToken) =>
            {
                var problemDetails = context.HttpContext.RequestServices
                    .GetRequiredService<IProblemDetailsService>();
                await problemDetails.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests.",
                        Detail = "The request rate limit has been exceeded.",
                    },
                });
            };

            options.AddPolicy(
                AuthenticationRateLimitPolicy,
                httpContext => CreateFixedWindowPartition(httpContext, AuthenticationPermitLimit));
        });

        return services;
    }

    private static RateLimitPartition<string> CreateFixedWindowPartition(
        HttpContext context,
        int permitLimit,
        TimeSpan? window = null)
    {
        // UseForwardedHeaders has already replaced RemoteIpAddress with the address Caddy
        // reported, so this is the real client. An unknown address falls into a shared bucket,
        // which is the conservative choice: it throttles rather than exempts.
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
