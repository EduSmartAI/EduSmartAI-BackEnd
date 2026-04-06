using AiService.Application.Features.AiSummary;
using AiService.Application.Interfaces;
using MediatR;

namespace AiService.Application.Handler.AiSummary
{
    /// <summary>
    /// Preview handler: ONLY generate AI feedback content, DO NOT save to DB.
    /// </summary>
    public class AiSummaryPreviewHandler(IAiSummaryService _aiSummaryService)
        : IRequestHandler<AiSummaryPreviewRequest, AiSummaryResponse>
    {
        public async Task<AiSummaryResponse> Handle(AiSummaryPreviewRequest request, CancellationToken cancellationToken)
        {
            // Reuse existing service logic to generate AI content
            var aiResponse = await _aiSummaryService.FeedBackCourseByAI(
                new AiSummaryRequest
                {
                    StudentId = request.StudentId,
                    CourseId = request.CourseId
                },
                cancellationToken);

            // Ensure preview semantics: do not persist anything here
            return aiResponse;
        }
    }
}


