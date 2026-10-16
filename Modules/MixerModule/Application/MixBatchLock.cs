using System.Buffers.Binary;
using System.Security.Cryptography;
using Npgsql;

namespace WBand.Modules.MixerModule.Application;

/// <summary>Owns one batch until disposal or worker disconnection, without a recovery delay.</summary>
public sealed class MixBatchLock : IAsyncDisposable
{
    private readonly NpgsqlConnection connection;
    private readonly CancellationTokenSource stop = new();
    private readonly CancellationTokenSource lost = new();
    private readonly Task monitor;

    private MixBatchLock(NpgsqlConnection connection)
    {
        this.connection = connection;
        monitor = MonitorAsync();
    }

    /// <summary>Cancelled if the database connection protecting this batch is lost.</summary>
    public CancellationToken LockLost => lost.Token;

    /// <summary>Attempts a nonblocking lock using a dedicated, unpooled database session.</summary>
    public static async Task<MixBatchLock?> TryAcquireAsync(string connectionString,
        Guid batchId, CancellationToken cancellationToken)
    {
        var options = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Pooling = false, Timeout = 10, CommandTimeout = 10,
        };
        var connection = new NpgsqlConnection(options.ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("select pg_try_advisory_lock(@key)", connection);
            command.Parameters.AddWithValue("key",
                BinaryPrimitives.ReadInt64LittleEndian(SHA256.HashData(batchId.ToByteArray())));
            if (await command.ExecuteScalarAsync(cancellationToken) is true)
                return new MixBatchLock(connection);
            await connection.DisposeAsync();
            return null;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private async Task MonitorAsync()
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
            while (await timer.WaitForNextTickAsync(stop.Token))
            {
                await using var command = new NpgsqlCommand("select 1", connection);
                await command.ExecuteScalarAsync(stop.Token);
            }
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        catch { await lost.CancelAsync(); }
    }

    /// <summary>Closes the physical connection, immediately releasing its database lock.</summary>
    public async ValueTask DisposeAsync()
    {
        await stop.CancelAsync();
        await monitor;
        await connection.DisposeAsync();
        stop.Dispose();
        lost.Dispose();
    }
}
