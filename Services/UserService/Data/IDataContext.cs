using Microsoft.EntityFrameworkCore;
using UserService.Domain;

namespace UserService.Data;

public interface IDataContext
{
    DbSet<User> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
