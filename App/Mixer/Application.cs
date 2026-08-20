using Spectre.Console;

namespace MixerApp;

public static class Application
{
    public static async Task<int> RunAsync(string[] args)
    {
        var parsed = CliParser.Parse(args);
        if (!parsed.Success)
        {
            AnsiConsole.MarkupLine($"[red]Ошибка:[/] {Markup.Escape(parsed.Error!)}");
            PrintHelp();
            return 2;
        }
        var options = parsed.Options!;
        if (options.ShowHelp) { PrintHelp(); return 0; }

        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) => { eventArgs.Cancel = true; cancellation.Cancel(); };
        Console.CancelKeyPress += cancelHandler;
        try { return await RunCoreAsync(options, cancellation.Token); }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow]Операция отменена.[/]");
            return 130;
        }
        catch (Exception exception)
        {
            AnsiConsole.MarkupLine($"[red]Ошибка:[/] {Markup.Escape(exception.Message)}");
            return 1;
        }
        finally { Console.CancelKeyPress -= cancelHandler; }
    }

    private static async Task<int> RunCoreAsync(MixerOptions options, CancellationToken cancellationToken)
    {
        AnsiConsole.Write(new FigletText("Mixer").Color(Color.Aqua));
        if (!await ExternalTools.IsAvailableAsync("ffmpeg", cancellationToken) ||
            !await ExternalTools.IsAvailableAsync("ffprobe", cancellationToken))
            throw new InvalidOperationException("ffmpeg и ffprobe должны быть установлены и доступны через PATH.");

        string inputDirectory;
        if (string.IsNullOrWhiteSpace(options.InputDirectory))
        {
            if (!ConsoleUi.CanPrompt)
            {
                PrintNonInteractiveHint();
                return 2;
            }

            while (true)
            {
                var enteredPath = AnsiConsole.Ask<string>("Папка с [aqua]дорожками[/]:");
                if (PathResolver.TryResolveExistingDirectory(enteredPath, out inputDirectory, out var pathError))
                    break;
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(pathError)}[/]");
            }
        }
        else if (!PathResolver.TryResolveExistingDirectory(options.InputDirectory, out inputDirectory, out var pathError))
        {
            AnsiConsole.MarkupLine($"[red]Ошибка:[/] {Markup.Escape(pathError)}");
            AnsiConsole.MarkupLine("Укажите существующую папку с аудиодорожками первым аргументом.");
            return 2;
        }

        var outputDirectory = PathResolver.ResolveOutputDirectory(
            options.OutputDirectory ?? Path.Combine(inputDirectory, "mix-output"));
        var tracks = TrackDiscovery.Discover(inputDirectory, outputDirectory);
        if (tracks.Count == 0) throw new InvalidOperationException("В папке нет поддерживаемых аудиофайлов.");

        var clickCandidates = TrackDiscovery.FindClickCandidates(tracks);
        if (!ConsoleUi.CanPrompt && clickCandidates.Count != 1)
        {
            PrintSupportTrackHint("клик", clickCandidates);
            return 2;
        }
        var click = ResolveSupportTrack("клик", clickCandidates, tracks, null);
        var guideCandidates = TrackDiscovery.FindGuideCandidates(tracks);
        if (!ConsoleUi.CanPrompt && guideCandidates.Count(track => track != click) != 1)
        {
            PrintSupportTrackHint("гайд", guideCandidates.Where(track => track != click).ToArray());
            return 2;
        }
        var guide = ResolveSupportTrack("гайд", guideCandidates, tracks, click);
        var assignments = new TrackAssignments(tracks, click, guide);
        if (assignments.Instruments.Count == 0)
            throw new InvalidOperationException("После назначения клика и гайда не осталось инструментальных дорожек.");

        PrintSummary(inputDirectory, outputDirectory, options, assignments);
        if (ConsoleUi.CanPrompt && !AnsiConsole.Confirm("Начать обработку?", defaultValue: true)) return 0;

        var jobs = MixJobFactory.Create(assignments, options.ForegroundDb, options.BackgroundDb);
        var existingAction = ResolveExistingFiles(jobs, outputDirectory);
        if (existingAction == ExistingFilesAction.Cancel) return 0;
        var allCommands = jobs.Select(job =>
            FfmpegCommandBuilder.Build(job, outputDirectory, Guid.NewGuid().ToString("N")));
        var commands = OutputPlanner.Select(allCommands, existingAction);
        if (commands.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Все результаты уже существуют, обрабатывать нечего.[/]");
            return 0;
        }

        var runner = new FfmpegRunner(Path.Combine(outputDirectory, "ffmpeg.log"));
        string? failure = null;
        await AnsiConsole.Progress().AutoClear(false)
            .Columns(new SpinnerColumn(), new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn())
            .StartAsync(async context =>
            {
                var progress = context.AddTask("Подготовка миксов", maxValue: commands.Count);
                foreach (var command in commands)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress.Description = $"[aqua]{Markup.Escape(Path.GetFileNameWithoutExtension(command.FinalPaths.WavePath))}[/]";
                    var result = await runner.RunAsync(command, cancellationToken);
                    if (result.ExitCode != 0) { failure = LastUsefulLines(result.StandardError); break; }
                    progress.Increment(1);
                }
            });

        if (failure is not null)
        {
            AnsiConsole.MarkupLine($"[red]FFmpeg завершился с ошибкой.[/]\n{Markup.Escape(failure)}");
            AnsiConsole.MarkupLine($"Диагностика: [blue]{Markup.Escape(Path.Combine(outputDirectory, "ffmpeg.log"))}[/]");
            return 1;
        }
        AnsiConsole.MarkupLine($"[green]Готово.[/] Результаты: [blue]{Markup.Escape(outputDirectory)}[/]");
        return 0;
    }

    private static AudioTrack? ResolveSupportTrack(string role, IReadOnlyList<AudioTrack> candidates,
        IReadOnlyList<AudioTrack> allTracks, AudioTrack? excluded)
    {
        var eligibleCandidates = candidates.Where(track => track != excluded).ToArray();
        if (eligibleCandidates.Length == 1)
        {
            AnsiConsole.MarkupLine($"Найден {role}: [green]{Markup.Escape(eligibleCandidates[0].DisplayName)}[/]");
            return eligibleCandidates[0];
        }
        AnsiConsole.MarkupLine(eligibleCandidates.Length > 1
            ? $"[yellow]Найдено несколько кандидатов на роль «{role}».[/]"
            : $"[yellow]Не удалось автоматически найти {role}.[/]");
        const string none = "<нет>";
        var names = new[] { none }.Concat(allTracks.Where(track => track != excluded).Select(track => track.DisplayName)).ToArray();
        var selected = AnsiConsole.Prompt(new SelectionPrompt<string>().Title($"Выберите [aqua]{role}[/]:")
            .PageSize(Math.Min(15, names.Length)).AddChoices(names));
        return selected == none ? null : allTracks.Single(track => track.DisplayName == selected);
    }

    private static ExistingFilesAction ResolveExistingFiles(IEnumerable<MixJob> jobs, string outputDirectory)
    {
        var anyExist = jobs.Any(job => OutputPlanner.Exists(new OutputPaths(
            Path.Combine(outputDirectory, "wav", $"{job.Name}.wav"),
            Path.Combine(outputDirectory, "mp3", $"{job.Name}.mp3"))));
        if (!anyExist) return ExistingFilesAction.Overwrite;
        if (!ConsoleUi.CanPrompt)
        {
            AnsiConsole.MarkupLine("[red]Результаты уже существуют, а интерактивный выбор недоступен. Запустите утилиту в терминале.[/]");
            return ExistingFilesAction.Cancel;
        }
        var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Часть результатов уже существует:")
            .AddChoices("Перезаписать все", "Пропустить существующие", "Отменить"));
        return choice switch
        {
            "Перезаписать все" => ExistingFilesAction.Overwrite,
            "Пропустить существующие" => ExistingFilesAction.Skip,
            _ => ExistingFilesAction.Cancel
        };
    }

    private static void PrintSummary(string inputDirectory, string outputDirectory, MixerOptions options,
        TrackAssignments assignments)
    {
        var table = new Table().Border(TableBorder.Rounded).AddColumn("Параметр").AddColumn("Значение");
        table.AddRow("Исходники", Markup.Escape(inputDirectory));
        table.AddRow("Результат", Markup.Escape(outputDirectory));
        table.AddRow("Клик", Markup.Escape(assignments.ClickTrack?.DisplayName ?? "нет"));
        table.AddRow("Гайд", Markup.Escape(assignments.GuideTrack?.DisplayName ?? "нет"));
        table.AddRow("Инструменты", string.Join("\n", assignments.Instruments.Select(track => Markup.Escape(track.DisplayName))));
        table.AddRow("Уровни", $"основной {options.ForegroundDb:0.###} dB / фон {options.BackgroundDb:0.###} dB");
        AnsiConsole.Write(table);
    }

    private static string LastUsefulLines(string error)
    {
        var lines = error.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        return string.Join(Environment.NewLine, lines.TakeLast(8));
    }

    private static void PrintHelp()
    {
        AnsiConsole.WriteLine("Использование:");
        AnsiConsole.WriteLine("  Mixer [input-directory] [--output <directory>] [--foreground-db <dB>] [--background-db <dB>]");
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine("По умолчанию: foreground = 0 dB, background = -40 dB, output = <input>/mix-output.");
    }

    private static void PrintNonInteractiveHint()
    {
        AnsiConsole.MarkupLine("[red]Интерактивный ввод недоступен в текущем окне запуска.[/]");
        AnsiConsole.MarkupLine("Откройте обычный терминал или передайте папку первым аргументом:");
        AnsiConsole.WriteLine("  Mixer.exe \"C:\\path\\to\\tracks\"");
    }

    private static void PrintSupportTrackHint(string role, IReadOnlyList<AudioTrack> candidates)
    {
        var reason = candidates.Count == 0 ? "не найдена" : "определена неоднозначно";
        AnsiConsole.MarkupLine($"[red]Дорожка «{Markup.Escape(role)}» {reason}, а интерактивный выбор недоступен.[/]");
        AnsiConsole.MarkupLine("Запустите утилиту в обычном терминале, чтобы выбрать файл или вариант «нет».");
    }
}
