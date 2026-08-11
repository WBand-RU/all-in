using WBand.Modules.FileModule.Services;

namespace WBand.Modules.FileModule.Endpoints;

public static class DownloadFileEndpoint
{
    public record DownloadFileRequest(Guid FileId);

    public record DownloadFileResponse(string SignedUrl);

    public static async Task<DownloadFileResponse> GetDownloadUrl(
        DownloadFileRequest request,
        IFileService fileService
    )
    {
        var url = await fileService.GetSignedDownloadUrl(request.FileId.ToString());
        return new(url);
    }
}
