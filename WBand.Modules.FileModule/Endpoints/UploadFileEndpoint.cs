using FluentValidation;
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
        var url = await fileService.GetSignedUploadUrl(request.FileId.ToString());
        return new(url);
    }
}

public sealed class UploadFileRequestValidator
    : AbstractValidator<UploadFileEndpoint.UploadFileRequest>
{
    public UploadFileRequestValidator()
    {
        RuleFor(request => request.FileId).NotEmpty();
    }
}
