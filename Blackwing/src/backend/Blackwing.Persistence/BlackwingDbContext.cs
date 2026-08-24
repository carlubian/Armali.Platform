using Microsoft.EntityFrameworkCore;

namespace Blackwing.Persistence;

/// <summary>
/// The single application database context. It carries no entities yet: the image catalogue
/// model and its migrations arrive in a later phase. It exists now so the readiness probe can
/// open a real PostgreSQL connection.
/// </summary>
public sealed class BlackwingDbContext(DbContextOptions<BlackwingDbContext> options)
    : DbContext(options)
{
}
