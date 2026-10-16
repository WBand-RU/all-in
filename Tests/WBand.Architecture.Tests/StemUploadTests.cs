using Marten;
using Microsoft.AspNetCore.Http;
using Moq;
using Shared.Services;
using WBand.Modules.BandModule.Contracts;
using WBand.Modules.FileModule.Contracts;
using WBand.Modules.SongModule.Contracts;
using WBand.Modules.StemModule.Domain;
using WBand.Modules.StemModule.Endpoints;
using Wolverine;
using Wolverine.Marten;
using Xunit;

namespace WBand.Architecture.Tests;

public sealed class StemUploadTests
{
    private readonly Mock<IDocumentSession> session = new();
    private readonly Mock<IMessageBus> bus = new();
    private readonly Mock<IMartenOutbox> outbox = new();
    private readonly Mock<ICurrentUser> user = new();
    private readonly StemUploadBatch batch = new()
    {
        Id = Guid.NewGuid(),
        SongId = Guid.NewGuid(),
        BandId = Guid.NewGuid(),
        CreatedBy = Guid.NewGuid(),
        ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
    };

    public StemUploadTests()
    {
        batch.Files = Enumerable
            .Range(0, 2)
            .Select(_ => new StemUploadDraft
            {
                Stem = new Stem { Id = Guid.NewGuid(), FileId = Guid.NewGuid() },
                Size = 10,
                Sha256 = new string('a', 64),
                FileName = "stem.wav",
                MimeType = "audio/wav",
            })
            .ToList();
        user.SetupGet(x => x.Id).Returns(batch.CreatedBy);
        session
            .Setup(x => x.LoadAsync<StemUploadBatch>(batch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(batch);
        bus.Setup(x =>
                x.InvokeAsync<bool>(
                    It.IsAny<CheckBandPermission>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                )
            )
            .ReturnsAsync(true);
        bus.Setup(x =>
                x.InvokeAsync<SongReference[]>(
                    It.IsAny<GetSongReferences>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                )
            )
            .ReturnsAsync([new SongReference(batch.SongId, batch.BandId, "Song", 1)]);
    }

    [Fact]
    public async Task InitializeBatch_PersistsCleanupManifestBeforeCreatingFiles_WithoutVisibleStems()
    {
        session
            .Setup(x => x.LoadAsync<StemUploadBatch>(batch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StemUploadBatch?)null);
        var manifestSaved = false;
        session
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => manifestSaved = true)
            .Returns(Task.CompletedTask);
        bus.Setup(x =>
                x.InvokeAsync<CreateFileUploadResult>(
                    It.IsAny<CreateFileUpload>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                )
            )
            .Returns(
                (object command, CancellationToken _, TimeSpan? _) =>
                {
                    Assert.True(manifestSaved);
                    var file = (CreateFileUpload)command;
                    return Task.FromResult(
                        CreateFileUploadResult.Success(
                            new FileUploadTicket(
                                file.FileId,
                                "http://storage/upload",
                                DateTimeOffset.UtcNow.AddMinutes(15),
                                "audio/wav",
                                10
                            )
                        )
                    );
                }
            );
        var inputs = batch
            .Files.Select(x => new CreateStemRequest(
                "Stem",
                StemKind.Other,
                null,
                x.FileName,
                x.MimeType,
                x.Size,
                x.Sha256
            ))
            .ToArray();

        Assert.Equal(
            200,
            (
                (IStatusCodeHttpResult)
                    await CreateStemBatchEndpoint.Post(
                        batch.SongId,
                        new CreateStemBatchRequest(batch.Id, inputs),
                        user.Object,
                        session.Object,
                        bus.Object,
                        default
                    )
            ).StatusCode
        );

        session.Verify(x => x.Store(It.IsAny<Stem[]>()), Times.Never);
        session.Verify(x => x.Store(It.IsAny<StemCollection[]>()), Times.Never);
        bus.Verify(
            x =>
                x.InvokeAsync<CreateFileUploadResult>(
                    It.IsAny<CreateFileUpload>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                ),
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task CompleteBatch_WhenAnyFileIsMissing_DoesNotPublishAnyStems()
    {
        var first = batch.Files[0];
        bus.Setup(x =>
                x.InvokeAsync<FileReference[]>(
                    It.IsAny<GetFileReferences>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                )
            )
            .ReturnsAsync([
                new(
                    first.Stem.FileId,
                    batch.BandId,
                    FileObjectStatus.Ready,
                    first.MimeType,
                    first.Size,
                    first.Sha256
                ),
            ]);

        Assert.Equal(409, ((IStatusCodeHttpResult)await Complete()).StatusCode);
        session.Verify(x => x.Store(It.IsAny<Stem[]>()), Times.Never);
        session.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        outbox.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteBatch_WhenAlreadyCommitted_SucceedsWithoutDuplicatingStems()
    {
        batch.Status = StemUploadStatus.Completed;
        Assert.Equal(204, ((IStatusCodeHttpResult)await Complete()).StatusCode);
        session.Verify(x => x.Store(It.IsAny<Stem[]>()), Times.Never);
        outbox.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AbortBatch_PersistsClosedStateBeforeDeletingEvenVerifiedFiles()
    {
        var saved = false;
        session
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => saved = true)
            .Returns(Task.CompletedTask);
        bus.Setup(x =>
                x.InvokeAsync<DeleteFileResult>(
                    It.IsAny<DeleteFile>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                )
            )
            .Returns(() =>
            {
                Assert.True(saved);
                Assert.Equal(StemUploadStatus.Aborted, batch.Status);
                return Task.FromResult(new DeleteFileResult(true));
            });

        Assert.Equal(204, ((IStatusCodeHttpResult)await Abort()).StatusCode);
        bus.Verify(
            x =>
                x.InvokeAsync<DeleteFileResult>(
                    It.IsAny<DeleteFile>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                ),
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task AbortBatch_WhenAlreadyCommitted_DoesNotDeleteSongFiles()
    {
        batch.Status = StemUploadStatus.Completed;
        Assert.Equal(409, ((IStatusCodeHttpResult)await Abort()).StatusCode);
        bus.Verify(
            x =>
                x.InvokeAsync<DeleteFileResult>(
                    It.IsAny<DeleteFile>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                ),
            Times.Never
        );
    }

    [Theory]
    [InlineData(FileObjectStatus.Pending)]
    [InlineData(FileObjectStatus.Rejected)]
    public void AllFilesReady_WhenOneFileIsNotVerified_ReturnsFalse(FileObjectStatus status)
    {
        var references = batch
            .Files.Select(x => new FileReference(
                x.Stem.FileId,
                batch.BandId,
                FileObjectStatus.Ready,
                x.MimeType,
                x.Size,
                x.Sha256
            ))
            .ToArray();
        references[1] = references[1] with { Status = status };
        Assert.False(StemUploadRules.AllFilesReady(batch, references));
    }

    [Fact]
    public void AllFilesReady_RequiresMatchingBandAndChecksumForEveryFile()
    {
        var references = batch
            .Files.Select(x => new FileReference(
                x.Stem.FileId,
                batch.BandId,
                FileObjectStatus.Ready,
                x.MimeType,
                x.Size,
                x.Sha256
            ))
            .ToArray();
        Assert.True(StemUploadRules.AllFilesReady(batch, references));
        references[1] = references[1] with { BandId = Guid.NewGuid() };
        Assert.False(StemUploadRules.AllFilesReady(batch, references));
    }

    private Task<IResult> Complete() =>
        CompleteStemBatchEndpoint.Post(
            batch.SongId,
            batch.Id,
            user.Object,
            session.Object,
            bus.Object,
            outbox.Object,
            default
        );

    private Task<IResult> Abort() =>
        AbortStemBatchEndpoint.Delete(
            batch.SongId,
            batch.Id,
            user.Object,
            session.Object,
            bus.Object,
            default
        );
}
