using BuildingBlocks.Messaging.Events.StudentService.Dashboards.CourseService;

namespace Course.Application.Interfaces
{
	public interface IExternalCourseService
	{
		Task<GetCourseModuleDashboardEventResponse> GetCourseModuleDashboardAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken);
	}
}
