using AiService.Application.Features.AiSummary;
using AiService.Application.Interfaces;
using MediatR;

namespace AiService.Application.Handler.AiSummary
{
    public class AiSummaryHandler(IAiSummaryService _aiSummaryService) : IRequestHandler<AiSummaryRequest, AiSummaryResponse>
    {
        public Task<AiSummaryResponse> Handle(AiSummaryRequest request, CancellationToken cancellationToken)
        {
            var response = _aiSummaryService.FeedBackCourseByAI(request, cancellationToken);
            return response;
        }
    }
}
