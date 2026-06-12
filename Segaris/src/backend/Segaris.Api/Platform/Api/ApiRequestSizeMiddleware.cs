using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace Segaris.Api.Platform.Api;

internal sealed class ApiRequestSizeMiddleware(RequestDelegate next)
{
    internal const long DefaultMaximumRequestBodySize = 1024 * 1024;

    public async Task InvokeAsync(HttpContext context, IProblemDetailsService problemDetailsService)
    {
        var endpointLimit = context.GetEndpoint()?.Metadata.GetMetadata<ApiRequestBodyLimit>()?.MaximumBytes;
        var maximumBytes = endpointLimit ?? DefaultMaximumRequestBodySize;
        var requestSizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (requestSizeFeature is { IsReadOnly: false })
        {
            requestSizeFeature.MaxRequestBodySize = maximumBytes;
        }

        if (context.Request.Path.StartsWithSegments("/api", StringComparison.Ordinal)
            && context.Request.ContentLength > maximumBytes)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status413PayloadTooLarge,
                    Title = "The request body is too large.",
                    Detail = $"This request body is limited to {maximumBytes} bytes.",
                },
            });
            return;
        }

        await next(context);
    }
}
