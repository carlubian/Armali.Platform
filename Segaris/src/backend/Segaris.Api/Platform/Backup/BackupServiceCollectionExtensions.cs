using Segaris.Api.Platform.Jobs;

namespace Segaris.Api.Platform.Backup;

internal static class BackupServiceCollectionExtensions
{
    public static IServiceCollection AddSegarisBackup(this IServiceCollection services)
    {
        services.AddSingleton<BackupPaths>();
        services.AddScoped<IPostgresDumpRunner, PgDumpProcessRunner>();
        services.AddScoped<BackupJobHandler>();
        services.AddSingleton(new JobTypeRegistration(
            BackupJobHandler.JobType,
            BackupJobHandler.ExclusivityKey,
            typeof(BackupJobHandler)));
        return services;
    }
}
