using AiService.Application.Features.AiRecommend;
using AiService.Application.Features.AiSummary;

namespace AiService.Application.Interfaces
{
    public interface IAiSummaryService
    {
        Task<AiSummaryResponse> FeedBackCourseByAI(AiSummaryRequest req, CancellationToken ct);
        
        Task<string> GenerateProgressFeedbackMarkdownAsync(AiSummaryFeedbackModuleDto req, CancellationToken ct = default);
        
        Task<AiRecommendImprovementResponse> GenerateLearningFeedbackMarkdownAsync(AiRecommendImprovementRequest req, CancellationToken ct = default);
        
        Task<SubjectAnalysisResponse> AnalyzeSubjectMarkAsync(SubjectAnalysisRequest req, CancellationToken ct = default);
        
        Task<SubjectAnalysisResponse> AnalyzeCourseSubjectAsync(Guid courseId, CancellationToken ct = default);
    }
}
