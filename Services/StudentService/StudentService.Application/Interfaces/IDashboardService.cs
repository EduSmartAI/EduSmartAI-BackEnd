using BuildingBlocks.Messaging.Events.StudentService.Dashboards.LessonDashboard;
using BuildingBlocks.Messaging.Events.StudentService.Dashboards.ModuleDashboard;
using StudentService.Application.Applications.Dashboards.Queries;

namespace StudentService.Application.Interfaces
{
	public interface IDashboardService
	{
		Task<GetModuleDashboardEventResponse> GetModuleDashboardAsync(GetModuleDashboardQuery request, CancellationToken ct = default);
		Task<GetLessonDashboardEventResponse> GetLessonDashboardAsync(GetLessonDashboardQuery request, CancellationToken ct = default);
	}
}
