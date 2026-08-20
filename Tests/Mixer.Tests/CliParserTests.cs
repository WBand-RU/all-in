using MixerApp;

namespace Mixer.Tests;

public sealed class CliParserTests
{
    [Fact]
    public void Parse_UsesDefaults()
    {
        var result = CliParser.Parse(["C:\\audio"]);

        Assert.True(result.Success);
        Assert.Equal("C:\\audio", result.Options!.InputDirectory);
        Assert.Null(result.Options.OutputDirectory);
        Assert.Equal(0, result.Options.ForegroundDb);
        Assert.Equal(-40, result.Options.BackgroundDb);
    }

    [Fact]
    public void Parse_ReadsAllOptions()
    {
        var result = CliParser.Parse(["tracks", "--output", "mixes", "--foreground-db", "-3.5", "--background-db", "-32"]);

        Assert.True(result.Success);
        Assert.Equal("mixes", result.Options!.OutputDirectory);
        Assert.Equal(-3.5, result.Options.ForegroundDb);
        Assert.Equal(-32, result.Options.BackgroundDb);
    }

    [Theory]
    [InlineData("--unknown")]
    [InlineData("--foreground-db")]
    [InlineData("--background-db=bad")]
    public void Parse_RejectsInvalidArguments(string argument)
    {
        var result = CliParser.Parse([argument]);
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }
}
