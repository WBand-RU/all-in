using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WBand.Dev.Services;

public static class AppExtensions
{
    public static IHost AddDotnet(
        this IHost app,
        string serviceName,
        string path,
        string? healthCheckUrl = null
    )
    {
        var manager = app.Services.GetRequiredService<ServiceInfoManager>();
        manager.Add(
            new(serviceName, "dotnet", ["watch", "--no-build", "--project", path], healthCheckUrl)
        );

        return app;
    }

    public static IHost AddBun(
        this IHost app,
        string serviceName,
        string path,
        string script,
        string? healthCheckUrl = null
    )
    {
        var manager = app.Services.GetRequiredService<ServiceInfoManager>();
        manager.Add(new(serviceName, "bun", ["--cwd", path, "run", script], healthCheckUrl));

        return app;
    }
}
