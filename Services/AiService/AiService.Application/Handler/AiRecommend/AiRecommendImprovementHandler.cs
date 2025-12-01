using AiService.Application.Features.AiRecommend;
using AiService.Application.Interfaces;
using MediatR;

namespace AiService.Application.Handler.AiRecommend
{
    public class AiRecommendImprovementHandler(IAiSummaryService aiSummaryService) : IRequestHandler<AiRecommendImprovementRequest, AiRecommendImprovementResponse>
    {
        public async Task<AiRecommendImprovementResponse> Handle(AiRecommendImprovementRequest request, CancellationToken cancellationToken)
        {
            var res = await aiSummaryService.GenerateLearningFeedbackMarkdownAsync(request, cancellationToken);
            return res;
        }
    }
}
