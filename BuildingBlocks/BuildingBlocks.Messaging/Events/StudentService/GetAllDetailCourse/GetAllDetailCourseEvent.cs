namespace BuildingBlocks.Messaging.Events.StudentService.GetAllDetailCourse
{
    /// <summary>
    /// This event use to get all course info include module and lesson
    /// </summary>
    /// <param name="CourseId"></param>
    /// <param name="StudentId"></param>
    public sealed record GetAllDetailCourseEvent(Guid CourseId, Guid StudentId);
}
