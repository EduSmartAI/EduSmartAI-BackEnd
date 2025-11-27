using AiService.Application.Features.AiRecommend;
using AiService.Application.Interfaces;
using MediatR;

namespace AiService.Application.Handler.AiRecommend
{
    public class AiRecommendImprovementHandler(IAiSummaryService _aiSummaryService) : IRequestHandler<AiRecommendImprovementRequest, AiRecommendImprovementResposne>
    {
        public async Task<AiRecommendImprovementResposne> Handle(AiRecommendImprovementRequest request, CancellationToken cancellationToken)
        {
            var res = await _aiSummaryService.GenerateLearningFeedbackMarkdownAsync(request);
            return res;
        }
    }
}
