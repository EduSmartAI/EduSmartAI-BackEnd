using BaseService.Common.ApiEntities;

namespace AiService.Application.Features.AiSummary
{
    public record AiSummaryFeedbackModuleResponse : AbstractApiResponse<string>
    {
        public override string Response { get; set; } = string.Empty;
    }
}
