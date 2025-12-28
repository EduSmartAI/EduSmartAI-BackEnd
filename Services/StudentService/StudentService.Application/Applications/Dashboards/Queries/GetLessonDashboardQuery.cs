using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.StudentService.Dashboards.LessonDashboard;

namespace StudentService.Application.Applications.Dashboards.Queries
{
	public record GetLessonDashboardQuery(Guid CourseId) : IQuery<GetLessonDashboardEventResponse>;
}
