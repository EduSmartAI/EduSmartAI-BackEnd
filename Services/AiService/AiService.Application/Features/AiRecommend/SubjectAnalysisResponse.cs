using BaseService.Common.ApiEntities;

namespace AiService.Application.Features.AiRecommend
{
    public record SubjectAnalysisResponse : AbstractApiResponse<SubjectAnalysisDto>
    {
        public override SubjectAnalysisDto Response { get; set; } = null!;
    }

    public class SubjectAnalysisDto
    {
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public double Mark { get; set; }
        public string ImprovementAnalysis { get; set; } = string.Empty;
        public List<DependentSubjectWarning> DependentWarnings { get; set; } = new();
    }

    public class DependentSubjectWarning
    {
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public int? SemesterIndex { get; set; }
        public string WarningMessage { get; set; } = string.Empty;
    }
}

