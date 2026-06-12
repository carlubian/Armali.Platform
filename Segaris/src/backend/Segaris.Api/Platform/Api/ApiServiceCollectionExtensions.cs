using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Routing;
using Segaris.Api.Platform.Identity;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Platform.Api;

internal static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddSegarisApiConventions(this IServiceCollection services)
    {
        services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.SerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });
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
        services.AddOpenApi();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddSingleton<IClock>(SystemClock.Instance);

        return services;
    }
}
