using WBand.Modules.SongModule.Application;
using WBand.Modules.SongModule.Domain;
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
            [new(1, 4, 4)], 2, [new("Verse", 1, 8)], "Lyrics", "C F G", SongStatus.Band);

        SongVersioning.Apply(song, snapshot);

        Assert.Equal("Old", song.Title);
        Assert.Equal(4, song.ContentVersion);
        Assert.Equal("Verse", song.Sections.Single().Name);
    }
}
