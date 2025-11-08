namespace BuildingBlocks.Messaging.Events.StudentService.GetOverviewCourse
{
    public sealed record GetOverviewCourseEvents(Guid courseId, Guid StudentId, int type);
}
