using WBand.Modules.SongModule.Application;
using WBand.Modules.SongModule.Domain;
using WBand.Modules.SongModule.Endpoints;
using Xunit;

namespace WBand.Architecture.Tests;

public sealed class SongVersioningTests
{
    [Fact]
    public void Snapshot_IsIndependentFromLaterSongChanges()
    {
        var song = new Song { Title = "Original", Authors = ["Author"] };

        var snapshot = SongVersioning.Snapshot(song);
        song.Authors.Add("Another");
        song.Title = "Changed";

        Assert.Equal("Original", snapshot.Title);
        Assert.Equal(["Author"], snapshot.Authors);
    }

    [Fact]
    public void Apply_RestoresEditableContent()
    {
        var song = new Song { Title = "Current", ContentVersion = 4 };
        var snapshot = new SongSnapshot("Old", ["Author"], "C", 120, [new(1, 120)],
            [new(1, 4, 4)], 2, SongStatus.Band);

        SongVersioning.Apply(song, snapshot);

        Assert.Equal("Old", song.Title);
        Assert.Equal(4, song.ContentVersion);
        Assert.Equal(SongStatus.Band, song.Status);
    }

    [Fact]
    public void Song_ContainsMetadataOnly_WhileSectionOwnsTextAndChords()
    {
        var section = new SongSection { Name = "Verse", Lyrics = "Text", Chords = "C F G" };

        Assert.Null(typeof(Song).GetProperty("Lyrics"));
        Assert.Null(typeof(Song).GetProperty("Chords"));
        Assert.Equal("Text", section.Lyrics);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(8, true)]
    public void SongSection_RequiresPositiveBarCount(int barCount, bool expectedValid)
    {
        var request = new CreateSongSectionRequest("Verse", 0, barCount, null, null);

        var result = new CreateSongSectionRequestValidator().Validate(request);

        Assert.Equal(expectedValid, result.IsValid);
    }
}
