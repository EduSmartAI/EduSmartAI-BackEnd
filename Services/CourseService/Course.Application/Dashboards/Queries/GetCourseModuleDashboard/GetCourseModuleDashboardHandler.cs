using BuildingBlocks.Messaging.Events.StudentService.Dashboards.CourseService;

namespace Course.Application.Dashboards.Queries.GetCourseModuleDashboard
{
	public class GetCourseModuleDashboardHandler(IExternalCourseService _externalCourseService) : IQueryHandler<GetCourseModuleDashboardQuery, GetCourseModuleDashboardEventResponse>
	{
		public async Task<GetCourseModuleDashboardEventResponse> Handle(GetCourseModuleDashboardQuery request, CancellationToken cancellationToken)
		{
			return await _externalCourseService.GetCourseModuleDashboardAsync(request.StudentId, request.CourseId, cancellationToken);
		}
	}
}
