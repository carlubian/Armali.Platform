using Microsoft.EntityFrameworkCore;

namespace Belfalas.Persistence;

public sealed class BelfalasDbContext(DbContextOptions<BelfalasDbContext> options) : DbContext(options);
