namespace Segaris.Api.Composition;

internal interface ISegarisModule
{
    void AddServices(IServiceCollection services, IConfiguration configuration);

    void MapEndpoints(IEndpointRouteBuilder endpoints);
}

