using BuildingBlocks.Messaging.Events.StudentService.Dashboards.CourseService;

namespace Course.Application.Dashboards.Queries.GetCourseModuleDashboard
{
	public record GetCourseModuleDashboardQuery(Guid StudentId, Guid CourseId) : IQuery<GetCourseModuleDashboardEventResponse>;
}
