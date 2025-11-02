using BuildingBlocks.Messaging.Events.StudentService.Dashboards.CourseService;

namespace Course.Application.Dashboards.Queries.GetCourseLessonDashboard
{
	public sealed class GetCourseLessonDashboardHandler(IExternalCourseService _externalCourseService) : IQueryHandler<GetCourseLessonDashboardQuery, GetCourseLessonDashboardEventResponse>
	{
		public async Task<GetCourseLessonDashboardEventResponse> Handle(GetCourseLessonDashboardQuery q, CancellationToken cancellationToken)
			=> await _externalCourseService.GetCourseLessonDashboardAsync(q.StudentId, q.CourseId, cancellationToken);
	}
}
