using Microsoft.AspNetCore.Antiforgery;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Identity.Security;

/// <summary>
/// Validates the antiforgery token pair for cookie-authenticated state-changing
/// requests. Safe read methods are not validated.
/// </summary>
internal sealed class AntiforgeryEndpointFilter(IAntiforgery antiforgery) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        if (!HttpMethods.IsGet(httpContext.Request.Method)
            && !HttpMethods.IsHead(httpContext.Request.Method)
            && !HttpMethods.IsOptions(httpContext.Request.Method)
            && !HttpMethods.IsTrace(httpContext.Request.Method)
            && !IsApiKeyAuthenticated(httpContext))
        {
            try
            {
                await antiforgery.ValidateRequestAsync(httpContext);
            }
            catch (AntiforgeryValidationException)
            {
                throw new ApiProblemException(
                    StatusCodes.Status400BadRequest,
                    ApiErrorCodes.BadRequest,
                    "Antiforgery validation failed.");
            }
        }

        return await next(context);
    }

    /// <summary>
    /// Antiforgery defends against a browser attaching ambient cookies to a
    /// cross-site request. An API key is carried explicitly in a header that no
    /// browser attaches on its own, so the token pair has nothing to protect.
    /// </summary>
    /// <remarks>
    /// The decision is keyed on the scheme that actually authenticated the request,
    /// never on the endpoint: a cookie-authenticated write is validated wherever it
    /// arrives, and no endpoint can opt itself out.
    /// </remarks>
    private static bool IsApiKeyAuthenticated(HttpContext httpContext) =>
        httpContext.User.Identity is { IsAuthenticated: true, AuthenticationType: ApiKeyAuthenticationDefaults.Scheme };
}
