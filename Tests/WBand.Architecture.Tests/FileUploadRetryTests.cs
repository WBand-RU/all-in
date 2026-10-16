using Marten;
using Microsoft.Extensions.Options;
using Moq;
using WBand.Modules.FileModule.Application;
using WBand.Modules.FileModule.Contracts;
using WBand.Modules.FileModule.Domain;
using WBand.Modules.FileModule.Infrastructure;
using WBand.Modules.FileModule.Services;
using Xunit;

namespace WBand.Architecture.Tests;

public sealed class FileUploadRetryTests
{
    [Fact]
    public async Task CreateFileUpload_WhenInitializationRetried_RefreshesUrlWithoutOverwritingMetadata()
    {
        var (session, storage, file, command) = Setup(FileObjectStatus.Pending);
        storage.Setup(x => x.GetSignedUploadUrl(file.ObjectKey, file.MimeType, file.Size)).ReturnsAsync("http://storage/fresh");
        var result = await CreateFileUploadHandler.Handle(command, session.Object, storage.Object,
            Options.Create(new FileStorageOptions { UploadExpirationSeconds = 900 }), default);
        Assert.Equal("http://storage/fresh", result.Ticket?.UploadUrl);
        Assert.Equal(FileObjectStatus.Pending, file.Status);
        session.Verify(x => x.Store(It.IsAny<FileObject[]>()), Times.Never);
        session.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(FileObjectStatus.Ready)]
    [InlineData(FileObjectStatus.Rejected)]
    public async Task CreateFileUpload_WhenFileAlreadyClosed_DoesNotReopenIt(FileObjectStatus status)
    {
        var (session, storage, file, command) = Setup(status);
        var result = await CreateFileUploadHandler.Handle(command, session.Object, storage.Object,
            Options.Create(new FileStorageOptions { UploadExpirationSeconds = 900 }), default);
        Assert.Equal("file_upload_conflict", result.Error);
        Assert.Equal(status, file.Status);
        storage.VerifyNoOtherCalls();
        session.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static (Mock<IDocumentSession>, Mock<IFileService>, FileObject, CreateFileUpload) Setup(FileObjectStatus status)
    {
        var file = new FileObject { Id = Guid.NewGuid(), BandId = Guid.NewGuid(), MimeType = "audio/wav",
            Size = 10, Sha256 = new string('a', 64), Status = status, ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) };
        file.ObjectKey = $"bands/{file.BandId:D}/songs/1/stems/{file.Id:D}/1.wav";
        var session = new Mock<IDocumentSession>();
        session.Setup(x => x.LoadAsync<FileObject>(file.Id, It.IsAny<CancellationToken>())).ReturnsAsync(file);
        return (session, new Mock<IFileService>(), file,
            new CreateFileUpload(file.Id, file.BandId, file.ObjectKey, "stem.wav", file.MimeType, file.Size, file.Sha256, Guid.Empty));
    }
}
