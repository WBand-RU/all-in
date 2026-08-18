using Microsoft.EntityFrameworkCore;

namespace WBand.Modules.UserModule.Data;

public interface IDataContext
{
    DbSet<User> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
