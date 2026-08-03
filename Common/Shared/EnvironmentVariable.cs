namespace Shared;

public static class EnvironmentVariable
{
    public static string Get(string key)
    {
        return Environment.GetEnvironmentVariable(key) ?? throw new Exception(key);
    }
}
