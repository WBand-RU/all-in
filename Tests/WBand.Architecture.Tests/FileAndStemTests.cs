using WBand.Modules.FileModule.Application;
using WBand.Modules.FileModule.Infrastructure;
using WBand.Modules.PlaylistModule.Contracts;
using WBand.Modules.SongModule.Contracts;
using WBand.Modules.StemModule.Application;
using WBand.Modules.StemModule.Endpoints;
using WBand.Modules.StemModule.Domain;
using Xunit;

namespace WBand.Architecture.Tests;

public sealed class FileAndStemTests
{
    private static readonly FileStorageOptions StorageOptions = new()
    {
        MaxFileSizeBytes = 1_000,
        AllowedMimeTypes = ["audio/wav", "audio/x-wav"],
    };

    [Fact]
    public void FileUpload_RejectsWrongMimeSizeAndChecksum()
    {
        Assert.Equal("file_mime_not_allowed", FileUploadRules.Validate(
            "audio/mpeg", 100, new string('a', 64), StorageOptions));
        Assert.Equal("file_size_not_allowed", FileUploadRules.Validate(
            "audio/wav", 1_001, new string('a', 64), StorageOptions));
        Assert.Equal("file_sha256_invalid", FileUploadRules.Validate(
            "audio/wav", 100, "not-a-checksum", StorageOptions));
    }

    [Fact]
    public void FileUpload_AcceptsWavWithinConfiguredLimit()
    {
        var error = FileUploadRules.Validate("audio/wav; charset=binary", 1_000,
            new string('A', 64), StorageOptions);

        Assert.Null(error);
    }

    [Fact]
    public void FileObjectKey_MustStayInsideBandAndFileScope()
    {
        var bandId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        Assert.True(FileUploadRules.IsSafeObjectKey(
            $"bands/{bandId:D}/songs/1/stems/{fileId:D}/1.wav", bandId, fileId));
        Assert.False(FileUploadRules.IsSafeObjectKey(
            $"bands/{Guid.NewGuid():D}/files/{fileId:D}/stem.wav", bandId, fileId));
        Assert.False(FileUploadRules.IsSafeObjectKey(
            $"bands/{bandId:D}/../{fileId:D}/stem.wav", bandId, fileId));
    }

    [Fact]
    public void StemUpload_AcceptsSupportedCompressedAudio()
    {
        var request = new CreateStemRequest("Guitar", StemKind.Guitar, null,
            "guitar.mp3", "audio/mpeg", 100, new string('a', 64));
        var validator = new CreateStemRequestValidator();

        Assert.True(validator.Validate(request).IsValid);
    }

    [Fact]
    public void StemReorder_RejectsDuplicateStemIdentifiers()
    {
        var stemId = Guid.NewGuid();
        var request = new ReorderStemsRequest(1,
            [new(stemId, null, 0, 1), new(stemId, Guid.NewGuid(), 1, 1)]);

        var result = new ReorderStemsRequestValidator().Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ContentEvents_ProducePlaybackCacheInvalidation()
    {
        var bandId = Guid.NewGuid();
        var songId = Guid.NewGuid();
        var playlistId = Guid.NewGuid();

        var song = PlaybackInvalidationHandlers.Handle(new SongChanged(songId, bandId, 7));
        var playlist = PlaybackInvalidationHandlers.Handle(
            new PlaylistChanged(playlistId, bandId, 4));

        Assert.Equal(("song", songId, 7L),
            (song.ResourceType, song.ResourceId, song.ContentVersion));
        Assert.Equal(("playlist", playlistId, 4L),
            (playlist.ResourceType, playlist.ResourceId, playlist.ContentVersion));
    }
}
