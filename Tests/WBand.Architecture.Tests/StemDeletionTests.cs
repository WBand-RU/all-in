using Marten;
using Microsoft.AspNetCore.Http;
using Moq;
using Shared.Services;
using WBand.Modules.BandModule.Contracts;
using WBand.Modules.FileModule.Contracts;
using WBand.Modules.SongModule.Contracts;
using WBand.Modules.StemModule.Contracts;
using WBand.Modules.StemModule.Domain;
using WBand.Modules.StemModule.Endpoints;
using Wolverine;
using Wolverine.Marten;
using Xunit;

namespace WBand.Architecture.Tests;

public sealed class StemDeletionTests
{
    private readonly Mock<IDocumentSession> session = new();
    private readonly Mock<IMessageBus> bus = new();
    private readonly Mock<IMartenOutbox> outbox = new();
    private readonly Mock<ICurrentUser> user = new();
    private readonly SongReference song = new(Guid.NewGuid(), Guid.NewGuid(), "Song", 1);
    private readonly Stem stem;

    public StemDeletionTests()
    {
        stem = new Stem
        {
            Id = Guid.NewGuid(),
            FileId = Guid.NewGuid(),
            SongId = song.SongId,
            BandId = song.BandId,
            Revision = 2,
        };
        user.SetupGet(x => x.Id).Returns(Guid.NewGuid());
        bus.Setup(x =>
                x.InvokeAsync<SongReference[]>(
                    It.IsAny<GetSongReferences>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                )
            )
            .ReturnsAsync([song]);
        bus.Setup(x =>
                x.InvokeAsync<bool>(
                    It.IsAny<CheckBandPermission>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                )
            )
            .ReturnsAsync(true);
        session
            .Setup(x => x.LoadAsync<Stem>(stem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stem);
    }

    [Fact]
    public async Task DeleteStem_WhenPermissionDenied_DoesNotDeleteFile()
    {
        bus.Setup(x =>
                x.InvokeAsync<bool>(
                    It.IsAny<CheckBandPermission>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                )
            )
            .ReturnsAsync(false);
        var result = await Delete();
        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>(result);
        VerifyNoDeletion();
    }

    [Fact]
    public async Task DeleteStem_WhenRevisionChanged_ReturnsConflictWithoutDeletingFile()
    {
        var result = await Delete(expectedRevision: 1);
        Assert.Equal(409, ((IStatusCodeHttpResult)result).StatusCode);
        VerifyNoDeletion();
    }

    [Fact]
    public async Task DeleteStem_WhenStemBelongsToAnotherSong_DoesNotDeleteFile()
    {
        stem.SongId = Guid.NewGuid();
        var result = await Delete();
        Assert.Equal(404, ((IStatusCodeHttpResult)result).StatusCode);
        VerifyNoDeletion();
    }

    [Fact]
    public async Task DeleteStem_WhenFileDeleted_RemovesStemAndPublishesCollectionChange()
    {
        var collection = new StemCollection
        {
            Id = song.SongId,
            SongId = song.SongId,
            BandId = song.BandId,
            ContentVersion = 5,
        };
        session
            .Setup(x => x.LoadAsync<StemCollection>(song.SongId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);
        bus.Setup(x =>
                x.InvokeAsync<DeleteFileResult>(
                    It.Is<DeleteFile>(c => c.FileId == stem.FileId && c.BandId == song.BandId),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                )
            )
            .ReturnsAsync(new DeleteFileResult(true));

        var result = await Delete();

        Assert.Equal(204, ((IStatusCodeHttpResult)result).StatusCode);
        session.Verify(x => x.Delete(stem), Times.Once);
        session.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        outbox.Verify(x => x.Enroll(session.Object), Times.Once);
        outbox.Verify(
            x =>
                x.PublishAsync(
                    It.Is<StemsChanged>(e => e.SongId == song.SongId && e.ContentVersion == 6),
                    It.IsAny<DeliveryOptions>()
                ),
            Times.Once
        );
    }

    private Task<IResult> Delete(long expectedRevision = 2) =>
        DeleteStemEndpoint.Delete(
            song.SongId,
            stem.Id,
            expectedRevision,
            user.Object,
            session.Object,
            bus.Object,
            outbox.Object,
            default
        );

    [Fact]
    public async Task DeleteStems_WhenCollectionChanged_DoesNotDeleteFiles()
    {
        SetupBatch(6);
        var result = await DeleteBatch();
        Assert.Equal(409, ((IStatusCodeHttpResult)result).StatusCode);
        VerifyNoDeletion();
    }

    [Fact]
    public async Task DeleteStems_WhenPermissionDenied_DoesNotDeleteFiles()
    {
        SetupBatch();
        bus.Setup(x =>
                x.InvokeAsync<bool>(
                    It.IsAny<CheckBandPermission>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                )
            )
            .ReturnsAsync(false);
        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>(await DeleteBatch());
        VerifyNoDeletion();
    }

    [Fact]
    public async Task DeleteStems_WhenOneRevisionChanged_DoesNotDeleteAnyFiles()
    {
        SetupBatch();
        stem.Revision = 3;
        Assert.Equal(409, ((IStatusCodeHttpResult)await DeleteBatch()).StatusCode);
        VerifyNoDeletion();
    }

    [Fact]
    public async Task DeleteStems_WhenGroupSelectionConfirmed_DeletesOnlySelectedFiles()
    {
        SetupBatch();
        stem.GroupId = Guid.NewGuid();
        bus.Setup(x =>
                x.InvokeAsync<DeleteFileResult>(
                    It.IsAny<DeleteFile>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                )
            )
            .ReturnsAsync(new DeleteFileResult(true));

        Assert.Equal(204, ((IStatusCodeHttpResult)await DeleteBatch()).StatusCode);

        session.Verify(
            x => x.LoadManyAsync<Stem>(It.Is<Guid[]>(ids => ids.Length == 1 && ids[0] == stem.Id)),
            Times.Once
        );
        bus.Verify(
            x =>
                x.InvokeAsync<DeleteFileResult>(
                    It.Is<DeleteFile>(c => c.FileId == stem.FileId),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                ),
            Times.Once
        );
        bus.Verify(
            x =>
                x.InvokeAsync<DeleteFileResult>(
                    It.Is<DeleteFile>(c => c.FileId != stem.FileId),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                ),
            Times.Never
        );
        session.Verify(x => x.Delete(stem), Times.Once);
        outbox.Verify(
            x =>
                x.PublishAsync(
                    It.Is<StemsChanged>(e => e.ContentVersion == 6),
                    It.IsAny<DeliveryOptions>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task DeleteStems_WhenSecondFileDeletionFails_KeepsStemMetadataForRetry()
    {
        SetupBatch();
        var second = new Stem
        {
            Id = Guid.NewGuid(),
            FileId = Guid.NewGuid(),
            SongId = song.SongId,
            BandId = song.BandId,
            Revision = 2,
        };
        session
            .Setup(x => x.LoadManyAsync<Stem>(It.IsAny<Guid[]>()))
            .ReturnsAsync(new List<Stem> { stem, second });
        bus.Setup(x =>
                x.InvokeAsync<DeleteFileResult>(
                    It.Is<DeleteFile>(c => c.FileId == stem.FileId),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                )
            )
            .ReturnsAsync(new DeleteFileResult(true));
        bus.Setup(x =>
                x.InvokeAsync<DeleteFileResult>(
                    It.Is<DeleteFile>(c => c.FileId == second.FileId),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                )
            )
            .ThrowsAsync(new IOException("Unavailable"));
        var request = new DeleteStemsRequest(5, [new(stem.Id, 2), new(second.Id, 2)]);

        await Assert.ThrowsAsync<IOException>(() =>
            DeleteStemsEndpoint.Delete(
                song.SongId,
                request,
                user.Object,
                session.Object,
                bus.Object,
                outbox.Object,
                default
            )
        );

        session.Verify(x => x.Delete(It.IsAny<Stem>()), Times.Never);
        session.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        outbox.VerifyNoOtherCalls();
    }

    [Fact]
    public void DeleteStems_WhenIdentifiersRepeat_RejectsRequest()
    {
        Assert.False(
            new DeleteStemsRequestValidator()
                .Validate(new DeleteStemsRequest(5, [new(stem.Id, 2), new(stem.Id, 2)]))
                .IsValid
        );
    }

    private void SetupBatch(long contentVersion = 5)
    {
        session
            .Setup(x => x.LoadAsync<StemCollection>(song.SongId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new StemCollection
                {
                    Id = song.SongId,
                    SongId = song.SongId,
                    BandId = song.BandId,
                    ContentVersion = contentVersion,
                }
            );
        session
            .Setup(x => x.LoadManyAsync<Stem>(It.IsAny<Guid[]>()))
            .ReturnsAsync(new List<Stem> { stem });
    }

    private Task<IResult> DeleteBatch() =>
        DeleteStemsEndpoint.Delete(
            song.SongId,
            new DeleteStemsRequest(5, [new(stem.Id, 2)]),
            user.Object,
            session.Object,
            bus.Object,
            outbox.Object,
            default
        );

    private void VerifyNoDeletion()
    {
        bus.Verify(
            x =>
                x.InvokeAsync<DeleteFileResult>(
                    It.IsAny<DeleteFile>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                ),
            Times.Never
        );
        session.Verify(x => x.Delete(It.IsAny<Stem>()), Times.Never);
        outbox.VerifyNoOtherCalls();
    }
}
