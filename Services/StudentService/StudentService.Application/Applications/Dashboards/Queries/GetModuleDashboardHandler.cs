using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.StudentService.Dashboards.ModuleDashboard;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.Dashboards.Queries
{
	public class GetModuleDashboardHandler(IDashboardService _dashboardService) : IQueryHandler<GetModuleDashboardQuery, GetModuleDashboardEventResponse>
	{
		public async Task<GetModuleDashboardEventResponse> Handle(GetModuleDashboardQuery request, CancellationToken cancellationToken)
		{
			return await _dashboardService.GetModuleDashboardAsync(request, cancellationToken);
		}
	}
}
