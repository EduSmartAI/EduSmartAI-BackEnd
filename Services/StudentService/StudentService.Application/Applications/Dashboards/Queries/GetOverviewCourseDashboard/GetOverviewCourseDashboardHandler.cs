using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.Dashboards.Queries.GetOverviewCourseDashboard
{
    public class GetOverviewCourseDashboardHandler(IDashboardService _dashboardService) : IQueryHandler<GetOverviewCourseDashboardQuery, GetOverviewCourseDashboardResponse>
    {
        public async Task<GetOverviewCourseDashboardResponse> Handle(GetOverviewCourseDashboardQuery request, CancellationToken cancellationToken)
        {
            return await _dashboardService.GetOverviewCourseDashboardAsync(request, cancellationToken);
        }
    }
}
