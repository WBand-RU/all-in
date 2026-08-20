namespace MixerApp;

public static class PathResolver
{
    public static bool TryResolveExistingDirectory(string? value, out string fullPath, out string error)
    {
        fullPath = string.Empty;
        var normalized = Normalize(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            error = "Путь не указан.";
            return false;
        }

        try
        {
            fullPath = Path.GetFullPath(ExpandHome(Environment.ExpandEnvironmentVariables(normalized)));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            error = $"Некорректный путь «{normalized}».";
            return false;
        }

        try
        {
            var attributes = File.GetAttributes(fullPath);
            if ((attributes & FileAttributes.Directory) == 0)
            {
                error = $"Указан файл, а нужна папка: «{fullPath}».";
                return false;
            }
        }
        catch (UnauthorizedAccessException)
        {
            error = $"Каталог существует, но нет доступа: «{fullPath}».";
            return false;
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException or IOException)
        {
            var relativeHint = Path.IsPathFullyQualified(normalized)
                ? string.Empty
                : $" Текущий рабочий каталог: «{Environment.CurrentDirectory}».";
            error = $"Каталог не найден: «{fullPath}».{relativeHint}";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static string ResolveOutputDirectory(string value) =>
        Path.GetFullPath(ExpandHome(Environment.ExpandEnvironmentVariables(Normalize(value))));

    private static string Normalize(string? value)
    {
        var result = (value ?? string.Empty).Trim().Trim('\u200B', '\u200E', '\u200F', '\uFEFF').Trim();

        // Explorer, messengers and IDEs may wrap copied paths in straight or typographic quotes.
        result = result.Trim('"', '\'', '“', '”', '„', '«', '»', '‹', '›').Trim();

        if (Uri.TryCreate(result, UriKind.Absolute, out var uri) && uri.IsFile)
            result = uri.LocalPath;

        return result;
    }

    private static string ExpandHome(string value)
    {
        if (value == "~")
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (value.StartsWith($"~{Path.DirectorySeparatorChar}") ||
            value.StartsWith($"~{Path.AltDirectorySeparatorChar}"))
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), value[2..]);
        return value;
    }
}
