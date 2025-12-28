using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.StudentService.Dashboards.ModuleDashboard;

namespace StudentService.Application.Applications.Dashboards.Queries
{
	public record GetModuleDashboardQuery(Guid CourseId) : IQuery<GetModuleDashboardEventResponse>;
}
