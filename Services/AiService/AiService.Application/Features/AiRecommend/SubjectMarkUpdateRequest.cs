using MediatR;

namespace AiService.Application.Features.AiRecommend
{
    public class SubjectMarkUpdateRequest : IRequest<SubjectMarkUpdateResponse>
    {
        public required string SubjectCode { get; set; }
        public required string SubjectName { get; set; }
        public double? OldMark { get; set; }
        public required double NewMark { get; set; }
        public required string NewAnalysis { get; set; }
        public string? CareerGoal { get; set; }
    }
}

