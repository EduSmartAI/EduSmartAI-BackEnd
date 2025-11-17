using Course.Application.Courses.Commands.CreateLessonTranscript;

namespace Course.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class LessonController(ISender sender) : ControllerBase
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		[HttpPost]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		public async Task<CreateLessonTranscriptResponse> CreateLessonTranscript([FromBody] CreateLessonTranscriptCommand request)
		{
			return await ApiControllerHelper.HandleRequest<CreateLessonTranscriptCommand, CreateLessonTranscriptResponse, string>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new CreateLessonTranscriptResponse()
			);
		}
	}
}
