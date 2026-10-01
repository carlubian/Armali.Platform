using Blackwing.Shared.Images;

namespace Blackwing.Api.Platform.Images;

internal static class ImageServiceCollectionExtensions
{
    /// <summary>
    /// Registers the image blob store, the processor and the ingestion service. The store and the
    /// processor are stateless and shared; ingestion is scoped because it works through the
    /// request's database context and identity.
    /// </summary>
    public static IServiceCollection AddBlackwingImages(this IServiceCollection services)
    {
        services.AddSingleton<IImageBlobStore, FileSystemImageBlobStore>();
        services.AddSingleton<IImageProcessor, SkiaImageProcessor>();
        services.AddScoped<ImageIngestionService>();
        return services;
    }
}
