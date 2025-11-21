namespace BuildingBlocks.Messaging.Events.StudentService.GetInfoInternalCourse
{
    public sealed record GetInfoInternalCourseEvents(List<Guid> CourseIds, Guid studentId);
}
