using Microsoft.EntityFrameworkCore;

namespace Segaris.Persistence;

public interface ISegarisModelContributor
{
    void Configure(ModelBuilder modelBuilder);
}
