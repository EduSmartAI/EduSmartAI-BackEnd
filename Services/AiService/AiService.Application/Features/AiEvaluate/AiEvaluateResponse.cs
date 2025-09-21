using BaseService.Common.ApiEntities;
using static AiService.Application.Contracts.AiRecommendContracts;

namespace AiService.Application.Features.AiEvaluate
{
    public record AiEvaluateResponse : AbstractApiResponse<EvaluateResult>
    {
        public override EvaluateResult Response { get; set; } = null!;
    }
}
