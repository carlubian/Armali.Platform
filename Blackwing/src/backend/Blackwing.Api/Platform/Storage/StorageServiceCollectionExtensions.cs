namespace Blackwing.Api.Platform.Storage;

internal static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddBlackwingStorage(this IServiceCollection services)
    {
        services.AddSingleton<BlackwingStoragePaths>();
        return services;
    }

    /// <summary>
    /// Creates the configured storage directories before the host starts serving requests.
    /// </summary>
    public static void EnsureBlackwingStorageDirectories(this IServiceProvider services)
    {
        services.GetRequiredService<BlackwingStoragePaths>().EnsureCreated();
    }
}
