using BuildingBlocks.Messaging.Events.StudentService.Dashboards.CourseService;
using Course.Application.Dashboards.Queries.GetCourseLessonDashboard;
using Course.Application.Dashboards.Queries.GetCourseModuleDashboard;

namespace Course.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class TestsController(ISender sender) : ControllerBase
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		[HttpGet]
		[SwaggerOperation(
			Summary = "Get course module dashboard for a student",
			Description = "Retrieve the module dashboard information for a specific student and course"
		)]
		public async Task<GetCourseModuleDashboardEventResponse> ProcessGetCourseModuleDashboard([FromQuery] Guid studentId, [FromQuery] Guid courseId)
		{
			var request = new GetCourseModuleDashboardQuery(studentId, courseId);
			return await ApiControllerHelper.HandleRequest<GetCourseModuleDashboardQuery, GetCourseModuleDashboardEventResponse, CourseModuleDashboardContract>(
			request,
			_logger,
			ModelState,
			async () => await sender.Send(request),
			new GetCourseModuleDashboardEventResponse());
		}

		[HttpGet("[action]")]
		public async Task<GetCourseLessonDashboardEventResponse> ProcessGetCourseLessonDashboard([FromQuery] Guid studentId, [FromQuery] Guid courseId)
		{
			var request = new GetCourseLessonDashboardQuery(studentId, courseId);
			return await ApiControllerHelper.HandleRequest<GetCourseLessonDashboardQuery, GetCourseLessonDashboardEventResponse, CourseLessonDashboardContract>(
			request,
			_logger,
			ModelState,
			async () => await sender.Send(request),
			new GetCourseLessonDashboardEventResponse());
		}
	}
}
