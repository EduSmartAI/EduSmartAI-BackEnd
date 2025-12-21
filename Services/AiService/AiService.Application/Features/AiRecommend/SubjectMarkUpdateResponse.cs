using BaseService.Common.ApiEntities;

namespace AiService.Application.Features.AiRecommend
{
    public record SubjectMarkUpdateResponse : AbstractApiResponse<SubjectMarkUpdateDto>
    {
        public override SubjectMarkUpdateDto Response { get; set; } = null!;
    }

    public class SubjectMarkUpdateDto
    {
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public double OldMark { get; set; }
        public double NewMark { get; set; }
        public double MarkImprovement { get; set; }
        public string ImprovementAnalysis { get; set; } = string.Empty;
        public string ComparisonAnalysis { get; set; } = string.Empty;
        public List<DependentSubjectWarning> DependentWarnings { get; set; } = new();
    }
}

