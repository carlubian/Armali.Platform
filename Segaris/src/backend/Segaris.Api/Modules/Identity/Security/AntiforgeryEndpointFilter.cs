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
            && !HttpMethods.IsTrace(httpContext.Request.Method))
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
}
