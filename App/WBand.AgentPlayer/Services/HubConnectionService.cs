using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WBand.AgentPlayer.Models;

namespace WBand.AgentPlayer.Services;

/// <summary>
/// Service for managing SignalR hub connection
/// </summary>
public sealed class HubConnectionService : BackgroundService
{
    private readonly ILogger<HubConnectionService> logger;
    private readonly AgentSettings settings;
    private readonly AudioPlaybackService audioService;
    private HubConnection? hubConnection;

    public HubConnectionService(
        ILogger<HubConnectionService> logger,
        IOptions<AgentSettings> settings,
        AudioPlaybackService audioService
    )
    {
        this.logger = logger;
        this.settings = settings.Value;
        this.audioService = audioService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConnectAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (hubConnection?.State == HubConnectionState.Disconnected)
            {
                logger.LogWarning("Connection lost. Attempting to reconnect...");
                await ConnectAsync(stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            hubConnection = new HubConnectionBuilder()
                .WithUrl(
                    $"{settings.ServerUrl}/agent-hub",
                    options =>
                    {
                        options.Headers["Authorization"] = $"Bearer {settings.AgentToken}";
                        options.Headers["X-Band-Id"] = settings.BandId;
                        options.Headers["X-Agent-Name"] = settings.AgentName;
                    }
                )
                .WithAutomaticReconnect()
                .Build();

            // Register handlers
            RegisterHandlers();

            await hubConnection.StartAsync(cancellationToken);
            logger.LogInformation("Connected to server hub at {ServerUrl}", settings.ServerUrl);

            // Register agent
            await hubConnection.InvokeAsync(
                "RegisterAgent",
                settings.BandId,
                settings.AgentName,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to server hub");
        }
    }

    private void RegisterHandlers()
    {
        if (hubConnection == null)
            return;

        hubConnection.On<PlaybackCommand>(
            "ExecuteCommand",
            async (command) =>
            {
                logger.LogInformation("Received command: {Command}", command.Command);

                try
                {
                    switch (command.Command.ToLower())
                    {
                        case "play":
                            if (!string.IsNullOrEmpty(command.TrackId))
                            {
                                await audioService.LoadTrackAsync(command.TrackId);
                            }
                            audioService.Play();
                            break;

                        case "pause":
                            audioService.Pause();
                            break;

                        case "stop":
                            audioService.Stop();
                            break;

                        case "seek":
                            if (command.Position.HasValue)
                            {
                                audioService.Seek(command.Position.Value);
                            }
                            break;

                        case "volume":
                            if (command.Volume.HasValue)
                            {
                                audioService.SetVolume(command.Volume.Value);
                            }
                            break;

                        case "mixer":
                            if (command.MixerSettings != null)
                            {
                                audioService.UpdateMixerSettings(command.MixerSettings);
                            }
                            break;
                    }

                    await SendStatusUpdateAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error executing command: {Command}", command.Command);
                    await SendErrorAsync(ex.Message);
                }
            }
        );

        hubConnection.On(
            "Ping",
            async () =>
            {
                await SendStatusUpdateAsync();
            }
        );
    }

    public async Task SendStatusUpdateAsync()
    {
        if (hubConnection?.State != HubConnectionState.Connected)
            return;

        var status = audioService.GetCurrentState();
        await hubConnection.InvokeAsync("UpdateStatus", status);
    }

    public async Task SendErrorAsync(string error)
    {
        if (hubConnection?.State != HubConnectionState.Connected)
            return;

        var status = audioService.GetCurrentState();
        status.ErrorMessage = error;
        await hubConnection.InvokeAsync("UpdateStatus", status);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (hubConnection != null)
        {
            await hubConnection.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}
