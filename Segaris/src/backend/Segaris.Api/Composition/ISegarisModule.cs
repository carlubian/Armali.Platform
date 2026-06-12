namespace Segaris.Api.Composition;

internal interface ISegarisModule
{
    string Name { get; }

    void AddServices(IServiceCollection services, IConfiguration configuration);

    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
