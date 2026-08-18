using Microsoft.Extensions.Hosting;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace WBand.Dev.Services;

internal sealed class ConsoleDashboard(
    ServiceInfoManager serviceInfoManager,
    ProcessManager processManager
) : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(1);
    private readonly Lock syncRoot = new();
    private string? selectedServiceName;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var inputTask = MonitorInputAsync(stoppingToken);

        try
        {
            await AnsiConsole
                .Live(CreateDisplay())
                .AutoClear(false)
                .Overflow(VerticalOverflow.Ellipsis)
                .StartAsync(async context =>
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        context.UpdateTarget(CreateDisplay());
                        await Task.Delay(RefreshInterval, stoppingToken);
                    }
                });
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        try
        {
            await inputTask;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task MonitorInputAsync(CancellationToken cancellationToken)
    {
        if (Console.IsInputRedirected)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!Console.KeyAvailable)
            {
                await Task.Delay(100, cancellationToken);
                continue;
            }

            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Escape)
            {
                lock (this.syncRoot)
                {
                    this.selectedServiceName = null;
                }

                continue;
            }

            var index = key.KeyChar - '1';
            var services = serviceInfoManager.Services.OrderBy(service => service.ServiceName).ToList();

            if (index is >= 0 and < 9 && index < services.Count)
            {
                lock (this.syncRoot)
                {
                    this.selectedServiceName = services[index].ServiceName;
                }
            }
        }
    }

    private IRenderable CreateDisplay()
    {
        var services = serviceInfoManager.Services.OrderBy(service => service.ServiceName).ToList();
        var selectedService = GetSelectedService(services);

        return selectedService is null ? CreateOverview(services) : CreateDetails(selectedService);
    }

    private ServiceInfo? GetSelectedService(IReadOnlyList<ServiceInfo> services)
    {
        lock (this.syncRoot)
        {
            return this.selectedServiceName is null
                ? null
                : services.FirstOrDefault(service => service.ServiceName == this.selectedServiceName);
        }
    }

    private IRenderable CreateOverview(IReadOnlyList<ServiceInfo> services)
    {
        var statuses = processManager.GetStatuses();
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold deepskyblue1]WBand development host[/]")
            .AddColumn("[bold]#[/]")
            .AddColumn("[bold]Service[/]")
            .AddColumn("[bold]Command[/]")
            .AddColumn("[bold]PID[/]")
            .AddColumn("[bold]Process[/]")
            .AddColumn("[bold]Health[/]")
            .AddColumn("[bold]Last output[/]");

        foreach (var (service, index) in services.Select((service, index) => (service, index)))
        {
            statuses.TryGetValue(service.ServiceName, out var process);

            table.AddRow(
                index < 9 ? $"[deepskyblue1]{index + 1}[/]" : "[grey]-[/]",
                Markup.Escape(service.ServiceName),
                FormatCommand(service),
                process?.ProcessId?.ToString() ?? "[grey]-[/]",
                FormatProcessStatus(process),
                service.Health ? "[green]Healthy[/]" : "[yellow]Waiting[/]",
                FormatOutput(process?.Output.LastOrDefault())
            );
        }

        table.Caption("[grey]Press 1-9 to inspect a service. Press Esc to return.[/]");
        return table;
    }

    private IRenderable CreateDetails(ServiceInfo service)
    {
        processManager.GetStatuses().TryGetValue(service.ServiceName, out var process);

        var summary = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold deepskyblue1]{Markup.Escape(service.ServiceName)}[/]")
            .AddColumn("[bold]Property[/]")
            .AddColumn("[bold]Value[/]");

        summary.AddRow("Command", FormatCommand(service));
        summary.AddRow("PID", process?.ProcessId?.ToString() ?? "[grey]-[/]");
        summary.AddRow("Process", FormatProcessStatus(process));
        summary.AddRow("Health", service.Health ? "[green]Healthy[/]" : "[yellow]Waiting[/]");
        summary.AddRow("Exit code", process?.ExitCode?.ToString() ?? "[grey]-[/]");

        var output = process?.Output ?? [];
        var outputText = output.Count == 0 ? "[grey]No output yet.[/]" : Markup.Escape(string.Join(Environment.NewLine, output));
        var outputPanel = new Panel(new Markup(outputText))
        {
            Header = new PanelHeader("Last 20 output lines"),
            Border = BoxBorder.Rounded,
        };

        var layout = new Grid();
        layout.AddRow(summary);
        layout.AddRow(outputPanel);
        layout.AddRow(new Markup("[grey]Press Esc to return to all services.[/]"));

        return layout;
    }

    private static string FormatProcessStatus(ManagedProcessStatus? process)
    {
        if (process is null)
        {
            return "[yellow]Starting[/]";
        }

        if (process.IsRunning)
        {
            return "[green]Running[/]";
        }

        return process.ExitCode is null
            ? "[red]Stopped[/]"
            : $"[red]Exited ({process.ExitCode})[/]";
    }

    private static string FormatOutput(string? output)
    {
        const int maxLength = 72;

        if (string.IsNullOrWhiteSpace(output))
        {
            return "[grey]-[/]";
        }

        var text = output.Length <= maxLength ? output : output[..(maxLength - 3)] + "...";
        return Markup.Escape(text);
    }

    private static string FormatCommand(ServiceInfo service)
    {
        const int maxLength = 48;
        var command = $"{service.Filename} {string.Join(" ", service.Arguments)}";
        var text = command.Length <= maxLength ? command : command[..(maxLength - 3)] + "...";

        return Markup.Escape(text);
    }
}
