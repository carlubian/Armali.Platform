using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Blackwing.Api.Platform.Api;

/// <summary>
/// Converts every unhandled exception into a problem response. Only an
/// <see cref="ApiProblemException"/> shapes its own payload; anything else collapses into a
/// generic 500 so that no internal detail, connection string, or stack frame reaches a client.
/// </summary>
internal sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var problem = exception switch
        {
            ApiProblemException apiProblem => CreateProblem(apiProblem),
            BadHttpRequestException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "The request is invalid.",
                Detail = "The request could not be bound to the expected contract.",
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = "The request could not be completed.",
            },
        };

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception while processing {RequestPath}", httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails CreateProblem(ApiProblemException exception)
    {
        var problem = new ProblemDetails
        {
            Status = exception.StatusCode,
            Title = exception.Title,
            Detail = exception.Detail,
        };
        problem.Extensions["code"] = exception.Code.Value;

        if (exception.Errors is not null)
        {
            problem.Extensions["errors"] = exception.Errors;
        }

        return problem;
    }
}
