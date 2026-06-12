using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Segaris.Api.Platform.Attachments;

internal sealed class AttachmentStorageHealthCheck(AttachmentStoragePaths paths) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var probePath = Path.Combine(paths.Root, $".readiness-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(paths.Root);
            await File.WriteAllBytesAsync(probePath, [], cancellationToken);
            File.Delete(probePath);
            return HealthCheckResult.Healthy("Attachment storage is writable.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return HealthCheckResult.Unhealthy("Attachment storage is not writable.", exception);
        }
        finally
        {
            try
            {
                File.Delete(probePath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
