using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace WBand.Persistence;

/// <summary>Shared SQL execution and explicit transaction writes for PostgreSQL contexts.</summary>
public abstract class RelationalDataContext(DbContextOptions options) : DbContext(options)
{
    /// <summary>Whether this context has an active database transaction.</summary>
    public bool HasTransaction => Database.CurrentTransaction is not null;

    /// <summary>Starts a transaction that rolls back on disposal unless committed.</summary>
    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default) => Database.BeginTransactionAsync(ct);

    /// <summary>Holds a PostgreSQL resource lock until the current transaction ends.</summary>
    public Task LockResourceAsync(string resource, CancellationToken ct = default)
    {
        RequireTransaction();
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);
        return ExecuteAsync("select pg_advisory_xact_lock(hashtextextended(@resource, 0))", new { resource }, ct);
    }

    /// <summary>Executes parameterized SQL using the context's connection and transaction.</summary>
    public Task<int> ExecuteAsync(string sql, object? parameters = null, CancellationToken ct = default) =>
        Database.ExecuteSqlRawAsync(sql, Parameters(parameters), ct);

    /// <summary>Reads at most one row without retaining a stale tracked copy.</summary>
    public async Task<T?> SingleOrDefaultAsync<T>(string sql, object? parameters = null, CancellationToken ct = default) =>
        (await QueryAsync<T>(sql, parameters, ct)).SingleOrDefault();

    /// <summary>Reads scalar values and SQL projections; subclasses map their own entities.</summary>
    public virtual async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null, CancellationToken ct = default) =>
        await Database.SqlQueryRaw<T>(sql, Parameters(parameters)).ToListAsync(ct);

    protected Task<List<T>> Read<T>(string sql, object[] args, CancellationToken ct) where T : class =>
        Set<T>().FromSqlRaw(sql, args).AsNoTracking().ToListAsync(ct);

    /// <summary>Finds an entity by its primary key, reading current database values.</summary>
    public Task<T?> FindAsync<T>(Guid id, CancellationToken ct = default) where T : class =>
        Set<T>().AsNoTracking().SingleOrDefaultAsync(entity => EF.Property<Guid>(entity, "Id") == id, ct);

    /// <summary>Inserts an entity in the active transaction.</summary>
    public async Task<int> InsertAsync<T>(T entity, CancellationToken ct = default) where T : class
    {
        RequireTransaction();
        Add(entity);
        var count = await SaveChangesAsync(ct);
        Entry(entity).State = EntityState.Detached;
        return count;
    }

    /// <summary>Updates an entity in the active transaction.</summary>
    public async Task<int> UpdateAsync<T>(T entity, CancellationToken ct = default) where T : class
    {
        RequireTransaction();
        Update(entity);
        var count = await SaveChangesAsync(ct);
        Entry(entity).State = EntityState.Detached;
        return count;
    }

    /// <summary>Deletes an entity by primary key in the active transaction.</summary>
    public Task<int> DeleteAsync<T>(Guid id, CancellationToken ct = default) where T : class
    {
        RequireTransaction();
        return Set<T>().Where(entity => EF.Property<Guid>(entity, "Id") == id).ExecuteDeleteAsync(ct);
    }

    protected void RequireTransaction()
    {
        if (!HasTransaction) throw new InvalidOperationException("Data writes require an explicit transaction.");
    }

    private static NpgsqlTypes.NpgsqlDbType ParameterType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(Guid)) return NpgsqlTypes.NpgsqlDbType.Uuid;
        if (type == typeof(string)) return NpgsqlTypes.NpgsqlDbType.Text;
        if (type == typeof(int) || type.IsEnum) return NpgsqlTypes.NpgsqlDbType.Integer;
        if (type == typeof(long)) return NpgsqlTypes.NpgsqlDbType.Bigint;
        if (type == typeof(bool)) return NpgsqlTypes.NpgsqlDbType.Boolean;
        if (type == typeof(DateTimeOffset)) return NpgsqlTypes.NpgsqlDbType.TimestampTz;
        throw new NotSupportedException($"SQL parameter type {type.Name} is not supported.");
    }

    protected static object[] Parameters(object? values) => values is null ? [] :
        values.GetType().GetProperties().Select(property =>
        {
            var value = property.GetValue(values);
            if (value is Enum) value = Convert.ToInt32(value);
            return (object)new NpgsqlParameter(property.Name, ParameterType(property.PropertyType)) { Value = value ?? DBNull.Value };
        }).ToArray();
}
