using MediatR;

namespace AiService.Application.Features.AiRecommend
{
    public class CourseSubjectAnalysisRequest : IRequest<SubjectAnalysisResponse>
    {
        public required Guid CourseId { get; set; }
    }
}




