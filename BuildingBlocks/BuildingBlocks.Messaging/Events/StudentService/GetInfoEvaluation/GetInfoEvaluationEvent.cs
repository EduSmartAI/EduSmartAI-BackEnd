namespace BuildingBlocks.Messaging.Events.StudentService.GetInfoEvaluation
{

    public sealed record GetInfoEvaluationEvent(Guid StudentId, Guid CourseId);
}
