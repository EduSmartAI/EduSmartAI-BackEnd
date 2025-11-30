using BaseService.Common.ApiEntities;

namespace AiService.Application.Features.AiRecommend
{
    public record AiRecommendImprovementResposne : AbstractApiResponse<AiAnalysisSubjectAndAbilityDto>
    {
        public override AiAnalysisSubjectAndAbilityDto Response { get; set; } = null!;
    }
    public class AiAnalysisSubjectAndAbilityDto
    {
        public string SummaryFeedback { get; set; } = string.Empty;
        public string HabitAndInterestAnalysis { get; set; } = string.Empty;
        public string Personality { get; set; } = string.Empty;
        public string LearningAbility { get; set; } = string.Empty;
        public required List<SubjectAnalysis> SubjectAnalyses { get; set; }
        public required List<AbilityAnalysis> AbilityAnalyses { get; set; }
        public List<SubjectWithoutMarkAnalysis> WithoutMarkAnalysis { get; set; } = new();
    }
    public class SubjectAnalysis
    {
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string AnalysisMarkdown { get; set; } = string.Empty;
    }
    public class AbilityAnalysis
    {
        public string Name { get; set; } = string.Empty;
        public string AnalysisMarkdown { get; set; } = string.Empty;
    }

    public class SubjectWithoutMarkAnalysis
    {
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string AnalysisMarkdown { get; set; } = string.Empty;
    }
}
