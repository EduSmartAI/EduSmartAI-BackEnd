using AiService.Application.Features.AiRecommend;
using AiService.Application.Interfaces;
using MediatR;

namespace AiService.Application.Handler.AiRecommend
{
    public class CourseSubjectAnalysisHandler : IRequestHandler<CourseSubjectAnalysisRequest, SubjectAnalysisResponse>
    {
        private readonly IAiSummaryService _aiSummaryService;

        public CourseSubjectAnalysisHandler(IAiSummaryService aiSummaryService)
        {
            _aiSummaryService = aiSummaryService;
        }

        public async Task<SubjectAnalysisResponse> Handle(CourseSubjectAnalysisRequest request, CancellationToken cancellationToken)
        {
            return await _aiSummaryService.AnalyzeCourseSubjectAsync(request.CourseId, cancellationToken);
        }
    }
}

