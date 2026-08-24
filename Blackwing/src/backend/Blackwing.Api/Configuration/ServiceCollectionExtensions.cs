using Microsoft.Extensions.Options;

namespace Blackwing.Api.Configuration;

internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Binds and validates every typed options area. Validation runs at host start so a bad
    /// configuration fails the process instead of surfacing on the first request.
    /// </summary>
    public static IServiceCollection AddBlackwingConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName), binder =>
            {
                binder.ErrorOnUnknownConfiguration = true;
            })
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<DatabaseOptions>, DatabaseOptionsValidator>();

        services
            .AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName), binder =>
            {
                binder.ErrorOnUnknownConfiguration = true;
            })
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<StorageOptions>, StorageOptionsValidator>();

        services
            .AddOptions<ObservabilityOptions>()
            .Bind(configuration.GetSection(ObservabilityOptions.SectionName), binder =>
            {
                binder.ErrorOnUnknownConfiguration = true;
            })
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<ObservabilityOptions>, ObservabilityOptionsValidator>();

        return services;
    }
}
