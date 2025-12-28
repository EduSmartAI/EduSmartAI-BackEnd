using BuildingBlocks.Messaging.Events.StudentService.Dashboards.CourseService;

namespace Course.Application.Dashboards.Queries.GetCourseLessonDashboard
{
	public sealed record GetCourseLessonDashboardQuery(Guid StudentId, Guid CourseId) : IQuery<GetCourseLessonDashboardEventResponse>;
}
