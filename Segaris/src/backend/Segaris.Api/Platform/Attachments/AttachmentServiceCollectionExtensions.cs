using Segaris.Shared.Attachments;

namespace Segaris.Api.Platform.Attachments;

internal static class AttachmentServiceCollectionExtensions
{
    public static IServiceCollection AddSegarisAttachments(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<AttachmentStoragePaths>();
        services.AddScoped<IAttachmentService, FileSystemAttachmentService>();
        services.AddScoped<AttachmentReconciliationService>();
        services.AddHealthChecks()
            .AddCheck<AttachmentStorageHealthCheck>("attachments", tags: ["ready"]);
        return services;
    }
}
