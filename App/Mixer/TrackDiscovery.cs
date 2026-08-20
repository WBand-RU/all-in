using System.Text.RegularExpressions;

namespace MixerApp;

public static partial class TrackDiscovery
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    { ".wav", ".mp3", ".flac", ".m4a", ".aac", ".ogg", ".opus", ".wma" };

    public static IReadOnlyList<AudioTrack> Discover(string inputDirectory, string outputDirectory)
    {
        var files = Directory.EnumerateFiles(inputDirectory, "*", SearchOption.TopDirectoryOnly)
            .Where(path => SupportedExtensions.Contains(Path.GetExtension(path)))
            .OrderBy(path => Path.GetFileName(path), StringComparer.CurrentCultureIgnoreCase).ToArray();
        var duplicateStems = files.GroupBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var tracks = new List<AudioTrack>(files.Length);

        foreach (var path in files)
        {
            var stem = Path.GetFileNameWithoutExtension(path);
            var proposed = duplicateStems.Contains(stem) ? $"{stem}_{Path.GetExtension(path).TrimStart('.')}" : stem;
            tracks.Add(new AudioTrack(Path.GetFullPath(path), Path.GetFileName(path),
                MakeUnique(SanitizeFileName(proposed), usedNames)));
        }
        return tracks;
    }

    public static IReadOnlyList<AudioTrack> FindClickCandidates(IEnumerable<AudioTrack> tracks) =>
        tracks.Where(track => ClickWord().IsMatch(Path.GetFileNameWithoutExtension(track.Path))).ToArray();
    public static IReadOnlyList<AudioTrack> FindGuideCandidates(IEnumerable<AudioTrack> tracks) =>
        tracks.Where(track => GuideWord().IsMatch(Path.GetFileNameWithoutExtension(track.Path))).ToArray();

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "track" : sanitized.Trim();
    }

    private static string MakeUnique(string proposed, ISet<string> used)
    {
        if (used.Add(proposed)) return proposed;
        for (var suffix = 2; ; suffix++)
        {
            var candidate = $"{proposed}_{suffix}";
            if (used.Add(candidate)) return candidate;
        }
    }

    [GeneratedRegex(@"(?<![\p{L}\p{Nd}])(?:click|клик)(?![\p{L}\p{Nd}])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ClickWord();
    [GeneratedRegex(@"(?<![\p{L}\p{Nd}])(?:guide|гайд)(?![\p{L}\p{Nd}])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GuideWord();
}
