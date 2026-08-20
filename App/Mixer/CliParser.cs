using System.Globalization;

namespace MixerApp;

public sealed record CliParseResult(MixerOptions? Options, string? Error)
{
    public bool Success => Options is not null && Error is null;
}

public static class CliParser
{
    public static CliParseResult Parse(IReadOnlyList<string> args)
    {
        string? input = null;
        string? output = null;
        var foreground = 0d;
        var background = -40d;

        for (var index = 0; index < args.Count; index++)
        {
            var argument = args[index];
            if (argument is "-h" or "--help")
                return new CliParseResult(new MixerOptions(null, null, ShowHelp: true), null);

            if (argument == "--output")
            {
                if (!TryReadValue(args, ref index, out output))
                    return Error("После --output нужно указать каталог.");
                continue;
            }

            if (argument is "--foreground-db" or "--background-db")
            {
                if (!TryReadValue(args, ref index, out var rawValue) ||
                    !double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var gain) ||
                    !double.IsFinite(gain) || gain is < -100 or > 20)
                    return Error($"Для {argument} укажите число от -100 до 20 dB, используя точку как разделитель.");

                if (argument == "--foreground-db") foreground = gain;
                else background = gain;
                continue;
            }

            if (argument.StartsWith('-')) return Error($"Неизвестный параметр: {argument}");
            if (input is not null) return Error("Можно указать только один входной каталог.");
            input = argument;
        }

        return new CliParseResult(new MixerOptions(input, output, foreground, background), null);
    }

    private static bool TryReadValue(IReadOnlyList<string> args, ref int index, out string? value)
    {
        if (index + 1 >= args.Count) { value = null; return false; }
        value = args[++index];
        return true;
    }

    private static CliParseResult Error(string message) => new(null, message);
}
