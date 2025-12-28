using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.AIService.AiRecommend;

public record SubjectMarkUpdateEventResponse : AbstractApiResponse<List<SubjectMarkUpdateAnalysisDto>>
{
    public override List<SubjectMarkUpdateAnalysisDto> Response { get; set; } = new();
}

public class SubjectMarkUpdateAnalysisDto
{
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public double? OldMark { get; set; }
    public double NewMark { get; set; }
    public double MarkImprovement { get; set; }
    public string ImprovementAnalysis { get; set; } = string.Empty;
    public string ComparisonAnalysis { get; set; } = string.Empty;
    public List<DependentSubjectWarning> DependentWarnings { get; set; } = new();
}

public class DependentSubjectWarning
{
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int? SemesterIndex { get; set; }
    public string WarningMessage { get; set; } = string.Empty;
}

