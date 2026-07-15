using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using Segaris.Api.Modules.Identity.Security;

namespace Segaris.Api.Platform.Mcp;

internal static class SegarisMcpEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapSegarisMcp(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<McpOptions>>().Value;
        if (!options.Enabled)
        {
            return endpoints;
        }

        endpoints
            .MapMcp(McpOptions.EndpointPath)
            .RequireAuthorization(policy =>
            {
                policy.AddAuthenticationSchemes(ApiKeyAuthenticationDefaults.Scheme);
                policy.RequireAuthenticatedUser();
            });

        return endpoints;
    }
}
