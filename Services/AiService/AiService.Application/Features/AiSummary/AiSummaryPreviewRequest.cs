using MediatR;

namespace AiService.Application.Features.AiSummary
{
    /// <summary>
    /// Preview request: generate AI feedback content only, does not persist to DB.
    /// </summary>
    public class AiSummaryPreviewRequest : IRequest<AiSummaryResponse>
    {
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
    }
}


