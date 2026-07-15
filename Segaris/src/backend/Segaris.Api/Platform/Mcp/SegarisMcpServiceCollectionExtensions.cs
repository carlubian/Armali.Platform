using ModelContextProtocol.AspNetCore;

namespace Segaris.Api.Platform.Mcp;

internal static class SegarisMcpServiceCollectionExtensions
{
    public static IServiceCollection AddSegarisMcp(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<McpOptions>()
            .Bind(configuration.GetSection(McpOptions.SectionName), binder =>
            {
                binder.ErrorOnUnknownConfiguration = true;
            })
            .ValidateOnStart();

        var builder = services.AddMcpServer();
        HttpMcpServerBuilderExtensions.WithHttpTransport(builder, _ => { });
        builder
            .AddAuthorizationFilters()
            .WithTools<SegarisMcpIdentityTools>();

        return services;
    }
}
