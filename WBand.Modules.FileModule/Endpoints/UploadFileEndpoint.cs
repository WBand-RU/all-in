using Microsoft.AspNetCore.Authorization;
using WBand.Modules.FileModule.Services;
using Wolverine.Http;

namespace WBand.Modules.FileModule.Endpoints;

public static class UploadFileEndpoint
{
    public record UploadFileRequest(Guid FileId);

    public record UploadFileResponse(string SignedUrl);

    [Authorize]
    [WolverinePost("/files/upload")]
    public static async Task<UploadFileResponse> GetUploadUrl(
        UploadFileRequest request,
        IFileService fileService
    )
    {
        ArgumentOutOfRangeException.ThrowIfEqual(request.FileId, Guid.Empty);
        var url = await fileService.GetSignedUploadUrl(request.FileId.ToString());
        return new(url);
    }
}
