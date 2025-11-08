using AiService.Application.Features.AiSummary;

namespace AiService.Application.Interfaces
{
    public interface IAiSummaryService
    {
        Task<AiSummaryResponse> FeedBackCourseByAI(AiSummaryRequest req, CancellationToken ct);
        Task<string> GenerateProgressFeedbackMarkdownAsync(AiSummaryFeedbackModuleDto req, CancellationToken ct = default);
    }
}
