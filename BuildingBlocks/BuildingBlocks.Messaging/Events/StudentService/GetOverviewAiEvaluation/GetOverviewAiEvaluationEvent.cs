using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.StudentService.GetOverviewAiEvaluation
{
    public sealed record GetOverviewAiEvaluationEvent(Guid StudentId, Guid CourseId);

    public sealed record GetOverviewAiEvaluationEventResponse : AbstractApiResponse<OverviewAiEvaluationDto>
    {
        public override OverviewAiEvaluationDto Response { get; set; } = new();
    }

    public sealed class OverviewAiEvaluationDto
    {
        public string Summary { get; set; } = string.Empty;
        public double AverageScore100Raw { get; set; }
        public double AverageScore100 { get; set; }
    }
}




