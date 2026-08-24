using System.Text.Json;
using Blackwing.Api.Platform.Identity;
using Blackwing.Shared.Identity;
using Blackwing.Shared.Time;
using Microsoft.AspNetCore.Http.Json;

namespace Blackwing.Api.Platform.Api;

internal static class ApiServiceCollectionExtensions
{
    /// <summary>
    /// Registers the shared API conventions: camelCase JSON, typed problem responses carrying a
    /// <c>code</c> and a <c>traceId</c>, and the ambient request services
    /// (<see cref="ICurrentUser"/> and <see cref="IClock"/>) the rest of the application needs.
    /// </summary>
    /// <remarks>
    /// OpenAPI is deliberately not registered here. <c>Program.cs</c> already calls
    /// <c>AddOpenApi()</c>, and registering the document provider twice produces a duplicate
    /// document at the same route.
    /// </remarks>
    public static IServiceCollection AddBlackwingApiConventions(this IServiceCollection services)
    {
        services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        });

        // A body that cannot be bound must fail loudly as a 400 problem instead of arriving at
        // the handler as a null argument and failing later as an opaque 500.
        services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                var statusCode = context.ProblemDetails.Status
                    ?? context.HttpContext.Response.StatusCode;

                context.ProblemDetails.Extensions.TryAdd(
                    "code",
                    ApiErrorCodes.ForStatus(statusCode).Value);
                context.ProblemDetails.Extensions["traceId"] =
                    context.HttpContext.TraceIdentifier;
            };
        });

        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddSingleton<IClock>(SystemClock.Instance);

        return services;
    }
}
