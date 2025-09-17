namespace UtilityService.Application.Contracts
{
    public sealed record UploadVideoRequested(
        string TempPath,
        string PublicId,
        string FileName
    );
}
