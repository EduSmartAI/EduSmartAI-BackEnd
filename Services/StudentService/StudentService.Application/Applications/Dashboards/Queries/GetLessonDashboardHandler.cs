using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.StudentService.Dashboards.LessonDashboard;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.Dashboards.Queries
{
	internal class GetLessonDashboardHandler(IDashboardService _dashboardService) : IQueryHandler<GetLessonDashboardQuery, GetLessonDashboardEventResponse>
	{
		public async Task<GetLessonDashboardEventResponse> Handle(GetLessonDashboardQuery request, CancellationToken cancellationToken)
		{
			return await _dashboardService.GetLessonDashboardAsync(request, cancellationToken);
		}
	}
}
