using MediatR;

namespace AiService.Application.Features.AiSummary
{
    public class AiSummaryRequest : IRequest<AiSummaryResponse>
    {
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
    }
}
