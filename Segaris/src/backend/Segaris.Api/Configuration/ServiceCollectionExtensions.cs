using Microsoft.Extensions.Options;

namespace Segaris.Api.Configuration;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSegarisConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (environment.IsDevelopment()
            && string.IsNullOrWhiteSpace(configuration[$"{DatabaseOptions.SectionName}:Provider"]))
        {
            configuration[$"{DatabaseOptions.SectionName}:Provider"] = "Sqlite";
        }

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

        return services;
    }
}
