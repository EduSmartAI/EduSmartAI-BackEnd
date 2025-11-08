using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.Dashboards.Queries.GetOverviewCourseDashboard
{
    public record GetOverviewCourseDashboardQuery(Guid CourseId) : IQuery<GetOverviewCourseDashboardResponse>;
}
