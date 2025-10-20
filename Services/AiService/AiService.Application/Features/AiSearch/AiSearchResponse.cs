using BaseService.Common.ApiEntities;

namespace AiService.Application.Features.AiSearch
{
    public record AiSearchResponse : AbstractApiResponse<string>
    {
        public override string Response { get; set; } = string.Empty;
    }
}
