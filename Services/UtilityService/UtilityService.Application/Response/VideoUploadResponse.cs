using BaseService.Common.ApiEntities;

namespace UtilityService.Application.Response
{
    public record VideoUploadResponse : AbstractApiResponse<string>
    {
        public override string Response { get; set; } = null!;
    }
}
