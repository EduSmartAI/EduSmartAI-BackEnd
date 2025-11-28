using BaseService.Common.ApiEntities;

namespace AiService.Application.Features.AiRecommend
{
    public record AiRecommendImprovementResposne : AbstractApiResponse<AiAnalysisSubjectAndAbilityDto>
    {
        public override AiAnalysisSubjectAndAbilityDto Response { get; set; } = null!;
    }
    public class AiAnalysisSubjectAndAbilityDto
    {
        public string summaryFeedback { get; set; } = string.Empty;
        public string habitAndInterestAnalysis { get; set; } = string.Empty;
        public string personality { get; set; } = string.Empty;
        public string learningAbility { get; set; } = string.Empty;
        public required List<SubjectAnalysis> subjectAnalyses { get; set; }
        public required List<AbilityAnalysis> abilityAnalyses { get; set; }
        public List<SubjectWithoutMarkAnalysis> withoutMarkAnalysis { get; set; } = new();
    }
    public class SubjectAnalysis
    {
        public string subjectCode { get; set; } = string.Empty;
        public string subjectName { get; set; } = string.Empty;
        public string analysisMarkdown { get; set; } = string.Empty;
    }
    public class AbilityAnalysis
    {
        public string name { get; set; } = string.Empty;
        public string analysisMarkdown { get; set; } = string.Empty;
    }

    public class SubjectWithoutMarkAnalysis
    {
        public string subjectCode { get; set; } = string.Empty;
        public string subjectName { get; set; } = string.Empty;
        public string analysisMarkdown { get; set; } = string.Empty;
    }
}
