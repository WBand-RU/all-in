using BandService.Domain;
using Microsoft.EntityFrameworkCore;

namespace BandService.Data;

public interface DataContext
{
    DbSet<Band> Bands { get; }
    DbSet<Invitation> Invitations { get; }
    DbSet<Member> Members { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
