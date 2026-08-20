using MixerApp;

namespace Mixer.Tests;

public sealed class PathResolverTests
{
    [Fact]
    public void TryResolveExistingDirectory_TrimsWhitespaceAndTypedQuotes()
    {
        using var directory = new TemporaryDirectory();

        var success = PathResolver.TryResolveExistingDirectory(
            $"  \"{directory.Path}\"  ", out var resolved, out var error);

        Assert.True(success, error);
        Assert.Equal(Path.GetFullPath(directory.Path), resolved);
    }

    [Theory]
    [InlineData("«", "»")]
    [InlineData("“", "”")]
    [InlineData("\"", "")]
    public void TryResolveExistingDirectory_AcceptsClipboardQuoteVariants(string opening, string closing)
    {
        using var directory = new TemporaryDirectory();

        var success = PathResolver.TryResolveExistingDirectory(
            $"\u200B{opening}{directory.Path}{closing}\u200E", out var resolved, out var error);

        Assert.True(success, error);
        Assert.Equal(Path.GetFullPath(directory.Path), resolved);
    }

    [Fact]
    public void TryResolveExistingDirectory_AcceptsFileUri()
    {
        using var directory = new TemporaryDirectory();

        var success = PathResolver.TryResolveExistingDirectory(
            new Uri(directory.Path).AbsoluteUri, out var resolved, out var error);

        Assert.True(success, error);
        Assert.Equal(Path.GetFullPath(directory.Path), resolved);
    }

    [Fact]
    public void TryResolveExistingDirectory_ExplainsEmptyAndMissingPaths()
    {
        Assert.False(PathResolver.TryResolveExistingDirectory("  ", out _, out var emptyError));
        Assert.Equal("Путь не указан.", emptyError);

        var missing = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}");
        Assert.False(PathResolver.TryResolveExistingDirectory(missing, out _, out var missingError));
        Assert.Contains(Path.GetFullPath(missing), missingError);
    }
}
