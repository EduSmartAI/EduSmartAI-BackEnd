using AiService.Application.Features.AiRecommend;
using AiService.Application.Interfaces;
using MediatR;

namespace AiService.Application.Handler.AiRecommend
{
    public class SubjectAnalysisHandler(IAiSummaryService aiSummaryService) : IRequestHandler<SubjectAnalysisRequest, SubjectAnalysisResponse>
    {
        public async Task<SubjectAnalysisResponse> Handle(SubjectAnalysisRequest request, CancellationToken cancellationToken)
        {
            var res = await aiSummaryService.AnalyzeSubjectMarkAsync(request, cancellationToken);
            return res;
        }
    }
}

