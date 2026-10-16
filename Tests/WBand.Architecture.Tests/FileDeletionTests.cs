using Marten;
using Moq;
using WBand.Modules.FileModule.Application;
using WBand.Modules.FileModule.Contracts;
using WBand.Modules.FileModule.Domain;
using WBand.Modules.FileModule.Services;
using Xunit;

namespace WBand.Architecture.Tests;

public sealed class FileDeletionTests
{
    private readonly Mock<IDocumentSession> session = new(MockBehavior.Strict);
    private readonly Mock<IFileService> storage = new(MockBehavior.Strict);
    private readonly FileObject file = new() { Id = Guid.NewGuid(), BandId = Guid.NewGuid(), ObjectKey = "source.wav" };

    [Fact]
    public async Task DeleteFile_WhenStorageSucceeds_DeletesObjectBeforeMetadata()
    {
        session.Setup(x => x.LoadAsync<FileObject>(file.Id, It.IsAny<CancellationToken>())).ReturnsAsync(file);
        var sequence = new MockSequence();
        storage.InSequence(sequence).Setup(x => x.DeleteAsync(file.ObjectKey, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        session.InSequence(sequence).Setup(x => x.Delete(file));
        session.InSequence(sequence).Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await DeleteFileHandler.Handle(new DeleteFile(file.Id, file.BandId), session.Object, storage.Object, default);

        Assert.True(result.Deleted);
        storage.Verify(x => x.DeleteAsync(file.ObjectKey, It.IsAny<CancellationToken>()), Times.Once);
        session.Verify(x => x.Delete(file), Times.Once);
        session.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteFile_WhenStorageFails_KeepsMetadataForRetry()
    {
        session.Setup(x => x.LoadAsync<FileObject>(file.Id, It.IsAny<CancellationToken>())).ReturnsAsync(file);
        storage.Setup(x => x.DeleteAsync(file.ObjectKey, It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("Unavailable"));

        await Assert.ThrowsAsync<IOException>(() => DeleteFileHandler.Handle(new DeleteFile(file.Id, file.BandId), session.Object, storage.Object, default));

        session.Verify(x => x.Delete(It.IsAny<FileObject>()), Times.Never);
        session.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteFile_WhenBandDoesNotOwnFile_DoesNotDeleteAnything()
    {
        session.Setup(x => x.LoadAsync<FileObject>(file.Id, It.IsAny<CancellationToken>())).ReturnsAsync(file);

        var result = await DeleteFileHandler.Handle(new DeleteFile(file.Id, Guid.NewGuid()), session.Object, storage.Object, default);

        Assert.False(result.Deleted);
        storage.VerifyNoOtherCalls();
        session.Verify(x => x.Delete(It.IsAny<FileObject>()), Times.Never);
    }

    [Fact]
    public async Task DeleteFile_WhenAlreadyDeleted_SucceedsWithoutStorageCall()
    {
        session.Setup(x => x.LoadAsync<FileObject>(file.Id, It.IsAny<CancellationToken>())).ReturnsAsync((FileObject?)null);
        var result = await DeleteFileHandler.Handle(new DeleteFile(file.Id, file.BandId), session.Object, storage.Object, default);

        Assert.True(result.Deleted);
        storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteFile_WhenCancelledUploadArrivesLate_DeletesObjectUsingRecordedKey()
    {
        var key = $"bands/{file.BandId:D}/songs/1/stems/{file.Id:D}/1.wav";
        session.Setup(x => x.LoadAsync<FileObject>(file.Id, It.IsAny<CancellationToken>())).ReturnsAsync((FileObject?)null);
        storage.Setup(x => x.DeleteAsync(key, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        Assert.True((await DeleteFileHandler.Handle(new DeleteFile(file.Id, file.BandId, key), session.Object, storage.Object, default)).Deleted);
        storage.Verify(x => x.DeleteAsync(key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteFile_WhenRecordedKeyIsOutsideBand_DoesNotDeleteObject()
    {
        session.Setup(x => x.LoadAsync<FileObject>(file.Id, It.IsAny<CancellationToken>())).ReturnsAsync((FileObject?)null);
        Assert.False((await DeleteFileHandler.Handle(new DeleteFile(file.Id, file.BandId, "other.wav"), session.Object, storage.Object, default)).Deleted);
        storage.VerifyNoOtherCalls();
    }
}
