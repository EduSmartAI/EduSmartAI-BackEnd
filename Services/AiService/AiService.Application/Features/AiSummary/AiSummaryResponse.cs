using BaseService.Common.ApiEntities;

namespace AiService.Application.Features.AiSummary
{
    public record AiSummaryResponse : AbstractApiResponse<string>
    {
        public override string Response { get; set; } = string.Empty;
    }
}
