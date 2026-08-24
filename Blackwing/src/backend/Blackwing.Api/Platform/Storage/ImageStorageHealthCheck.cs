using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Blackwing.Api.Platform.Storage;

/// <summary>
/// Readiness probe for the image volume. It writes and deletes an empty probe file so a
/// read-only or missing mount is reported before traffic reaches the instance.
/// </summary>
internal sealed class ImageStorageHealthCheck(BlackwingStoragePaths paths) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);

        var probePath = Path.Combine(paths.Images, $".readiness-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(paths.Images);
            await File.WriteAllBytesAsync(probePath, [], cancellationToken);
            return HealthCheckResult.Healthy("Image storage is writable.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return HealthCheckResult.Unhealthy("Image storage is not writable.", exception);
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
