using MediatR;

namespace AiService.Application.Features.AiRecommend
{
    public class SubjectAnalysisRequest : IRequest<SubjectAnalysisResponse>
    {
        public required string SubjectCode { get; set; }
        public required string SubjectName { get; set; }
        public required double Mark { get; set; }
        public string? MajorCode { get; set; }
        public string? CareerGoal { get; set; }
    }
}

