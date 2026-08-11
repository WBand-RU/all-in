using Microsoft.AspNetCore.Authorization;
using WBand.Modules.FileModule.Services;
using Wolverine.Http;

namespace WBand.Modules.FileModule.Endpoints;

public static class DownloadFileEndpoint
{
    public record DownloadFileRequest(Guid FileId);

    public record DownloadFileResponse(string SignedUrl);

    [Authorize]
    [WolverinePost("/files/download")]
    public static async Task<DownloadFileResponse> GetDownloadUrl(
        DownloadFileRequest request,
        IFileService fileService
    )
    {
        ArgumentOutOfRangeException.ThrowIfEqual(request.FileId, Guid.Empty);
        var url = await fileService.GetSignedDownloadUrl(request.FileId.ToString());
        return new(url);
    }
}
