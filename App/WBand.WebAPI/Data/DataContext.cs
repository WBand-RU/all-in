using Microsoft.EntityFrameworkCore;
using WBand.Persistence;

namespace WBand.WebAPI.Data;

/// <summary>Runs historical host migrations without owning application entities.</summary>
public sealed class DataContext(DbContextOptions<DataContext> options) : RelationalDataContext(options)
{
}
