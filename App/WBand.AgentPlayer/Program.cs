using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WBand.AgentPlayer.Models;
using WBand.AgentPlayer.Services;

var rootCommand = new RootCommand(
    "WBand Agent Player - Cross-platform audio player for worship bands"
);

var tokenOption = new Option<string>("token", ["--token", "-t"])
{
    Description = "The agent authentication token",
    Required = true,
};

var serverOption = new Option<string>("server", ["--server", "-s"])
{
    Description = "The server URL to connect to",
    DefaultValueFactory = (x) => "http://localhost",
};

rootCommand.Add(tokenOption);
rootCommand.Add(serverOption);

var parseResult = rootCommand.Parse(args);
if (parseResult.Errors.Count > 0)
{
    Console.WriteLine($"Invalid arguments: {string.Join(", ", parseResult.Errors)}");
    return;
}

await RunAgentAsync(parseResult.GetValue(tokenOption)!, parseResult.GetValue(serverOption)!);

async Task RunAgentAsync(string token, string server)
{
    var host = Host.CreateDefaultBuilder()
        .ConfigureAppConfiguration(
            (context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();

                // Override with command line arguments
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["AgentSettings:AgentToken"] = token,
                        ["AgentSettings:ServerUrl"] = server,
                    }
                );
            }
        )
        .ConfigureServices(
            (context, services) =>
            {
                services.Configure<AgentSettings>(
                    context.Configuration.GetSection("AgentSettings")
                );
                services.AddSingleton<AudioPlaybackService>();
                services.AddHostedService<HubConnectionService>();
            }
        )
        .ConfigureLogging(
            (context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            }
        )
        .Build();

    Console.WriteLine($"WBand Agent Player");
    Console.WriteLine($"==================");
    Console.WriteLine($"Server: {server}");
    Console.WriteLine();
    Console.WriteLine("Connecting to server...");
    Console.WriteLine("Press Ctrl+C to stop");
    Console.WriteLine();

    await host.RunAsync();
}
