using Microsoft.EntityFrameworkCore;

namespace Segaris.Persistence;

public static class SegarisDbContextOptions
{
    public const string PostgresMigrationsAssembly = "Segaris.Migrations.Postgres";
    public const string SqliteMigrationsAssembly = "Segaris.Migrations.Sqlite";

    public static void Configure(
        DbContextOptionsBuilder options,
        DatabaseProvider provider,
        string connectionString)
    {
        switch (provider)
        {
            case DatabaseProvider.Sqlite:
                options.UseSqlite(
                    connectionString,
                    sqlite => sqlite.MigrationsAssembly(SqliteMigrationsAssembly));
                break;
            case DatabaseProvider.Postgres:
                options.UseNpgsql(
                    connectionString,
                    postgres => postgres.MigrationsAssembly(PostgresMigrationsAssembly));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
        }
    }
}
