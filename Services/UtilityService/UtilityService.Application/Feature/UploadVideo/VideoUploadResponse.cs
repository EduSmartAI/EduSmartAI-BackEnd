using BaseService.Common.ApiEntities;

namespace UtilityService.Application.Feature.UploadVideo
{
    public record VideoUploadResponse : AbstractApiResponse<string>
    {
        public override string Response { get; set; } = null!;
    }
}
