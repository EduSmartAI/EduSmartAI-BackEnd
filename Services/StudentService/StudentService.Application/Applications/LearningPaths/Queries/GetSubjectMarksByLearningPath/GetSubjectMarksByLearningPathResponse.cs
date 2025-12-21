using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.LearningPaths.Queries.GetSubjectMarksByLearningPath
{
    public sealed record GetSubjectMarksByLearningPathResponse : AbstractApiResponse<List<SubjectMarkDto>>
    {
        public override List<SubjectMarkDto> Response { get; set; } = new();
    }

    public sealed class SubjectMarkDto
    {
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public double? OldMark { get; set; }
        public double? NewMark { get; set; }
        public string NewAnalysis { get; set; } = string.Empty;
        public string CareerGoal { get; set; } = string.Empty;
    }
}

