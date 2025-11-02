namespace BuildingBlocks.Messaging.Events.StudentService.Dashboards.LessonDashboard
{
	public sealed record GetLessonDashboardEvent(Guid StudentId, Guid CourseId);
}
