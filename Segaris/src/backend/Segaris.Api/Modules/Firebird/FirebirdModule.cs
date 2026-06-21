using Segaris.Api.Composition;
using Segaris.Api.Modules.Firebird.Mutations;
using Segaris.Api.Modules.Firebird.Persistence;
using Segaris.Api.Modules.Firebird.Queries;
using Segaris.Api.Modules.Firebird.Seeding;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Firebird;

/// <summary>
/// Business module for the household's register of known people. Wave 0 registers
/// the module and freezes the public contracts: person, avatar, username,
/// interaction, catalogue, birthday, attachment-owner, and launcher-attention
/// identities. Later waves add persistence, APIs, attention evaluation, and UI.
/// Firebird is independent from every other business module; it may consume
/// Configuration, Attachments, Identity, Launcher, and platform contracts.
/// </summary>
internal sealed class FirebirdModule : ISegarisModule
{
    public string Name => "Firebird";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISegarisModelContributor, FirebirdModelContributor>();
        services.AddScoped<FirebirdSeeder>();
        services.AddScoped<FirebirdCatalogReadService>();
        services.AddScoped<PersonCategoryManagementService>();
        services.AddScoped<UsernamePlatformManagementService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapFirebirdEndpoints();
    }
}
