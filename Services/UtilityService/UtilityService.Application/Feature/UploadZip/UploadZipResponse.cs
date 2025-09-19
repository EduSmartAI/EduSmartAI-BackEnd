using BaseService.Common.ApiEntities;

namespace UtilityService.Application.Feature.UploadZip
{
    public record UploadZipResponse : AbstractApiResponse<string>
    {
        public override string Response { get; set; } = null!;
    }
}
