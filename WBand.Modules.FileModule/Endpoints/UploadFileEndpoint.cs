using WBand.Modules.FileModule.Services;

namespace WBand.Modules.FileModule.Endpoints;

public static class UploadFileEndpoint
{
    public record UploadFileRequest(Guid FileId);

    public record UploadFileResponse(string SignedUrl);

    public static async Task<UploadFileResponse> GetUploadUrl(
        UploadFileRequest request,
        IFileService fileService
    )
    {
        var url = await fileService.GetSignedUploadUrl(request.FileId.ToString());
        return new(url);
    }
}
