using Testcontainers.PostgreSql;

namespace WBand.WebAPI.Tests;

internal static class PostgresDiagnostics
{
    public static async Task SaveLogs(PostgreSqlContainer container)
    {
        var (stdout, stderr) = await container.GetLogsAsync();
        var path = Path.Combine(Path.GetTempPath(), $"wband-tests-postgres-{container.Id}.log");
        await File.WriteAllTextAsync(path, stdout + stderr);
        Console.WriteLine($"PostgreSQL diagnostic log: {path}");
    }
}
