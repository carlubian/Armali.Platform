using System.Diagnostics;

namespace Blackwing.Api.Observability;

/// <summary>
/// Cross-cutting response conventions that make every request traceable and safe to
/// render. Applied once, early in the pipeline, so both successful responses and
/// error responses (including ProblemDetails) carry them.
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>
    /// Stamps a correlation id on the response and hardens content handling:
    /// <list type="bullet">
    /// <item><c>X-Trace-ID</c> — the active trace id, matching the <c>traceId</c> in any
    /// ProblemDetails body, so an operator can tie a user-reported error to logs.</item>
    /// <item><c>X-Content-Type-Options: nosniff</c> — image and JSON responses are served
    /// with the declared content type only; browsers must not sniff a different one.</item>
    /// </list>
    /// </summary>
    public static IApplicationBuilder UseBlackwingResponseContext(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["X-Trace-ID"] = Activity.Current?.Id ?? context.TraceIdentifier;
                headers.XContentTypeOptions = "nosniff";
                return Task.CompletedTask;
            });
            await next(context);
        });

    /// <summary>The correlation id surfaced to clients and written into ProblemDetails.</summary>
    public static string TraceId(this HttpContext context) => Activity.Current?.Id ?? context.TraceIdentifier;
}
