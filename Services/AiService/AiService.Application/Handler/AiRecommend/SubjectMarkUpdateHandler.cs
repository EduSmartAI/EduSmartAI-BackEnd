using AiService.Application.Features.AiRecommend;
using AiService.Application.Interfaces;
using MediatR;

namespace AiService.Application.Handler.AiRecommend
{
    public class SubjectMarkUpdateHandler(IAiSummaryService aiSummaryService) : IRequestHandler<SubjectMarkUpdateRequest, SubjectMarkUpdateResponse>
    {
        public async Task<SubjectMarkUpdateResponse> Handle(SubjectMarkUpdateRequest request, CancellationToken cancellationToken)
        {
            var res = await aiSummaryService.AnalyzeSubjectMarkUpdateAsync(request, cancellationToken);
            return res;
        }
    }
}

