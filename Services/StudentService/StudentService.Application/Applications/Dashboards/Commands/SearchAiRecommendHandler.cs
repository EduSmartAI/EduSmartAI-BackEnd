using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.Dashboards.Commands
{
    public class SearchAiRecommendHandler(IAiEvaluationService _aiEvaluationService) : ICommandHandler<SearchAiRecommendRequest, SearchAiRecommendResponse>
    {
        public async Task<SearchAiRecommendResponse> Handle(SearchAiRecommendRequest request, CancellationToken cancellationToken)
        {
            return await _aiEvaluationService.GenAndInsertImprovement(request.ImprovementId, cancellationToken);
        }
    }
}
