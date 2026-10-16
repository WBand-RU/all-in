using Npgsql;

namespace WBand.Modules.MixerModule.Application;

/// <summary>Recognizes transient faults even when a database rollback wraps the original error.</summary>
public static class MixRetryPolicy
{
    /// <summary>Returns whether retrying the operation can recover without changing its inputs.</summary>
    public static bool IsTransient(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            if (current is HttpRequestException or IOException or TimeoutException or OperationCanceledException
                || current is NpgsqlException { IsTransient: true }) return true;
        return false;
    }
}
