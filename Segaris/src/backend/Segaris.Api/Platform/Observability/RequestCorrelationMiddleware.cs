using System.Diagnostics;

namespace Segaris.Api.Platform.Observability;

internal sealed class RequestCorrelationMiddleware(RequestDelegate next)
{
    public const string ResponseHeaderName = "X-Trace-ID";

    public async Task InvokeAsync(HttpContext context, ILogger<RequestCorrelationMiddleware> logger)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        context.TraceIdentifier = traceId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[ResponseHeaderName] = traceId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["TraceId"] = traceId,
        }))
        {
            await next(context);
        }
    }
}
