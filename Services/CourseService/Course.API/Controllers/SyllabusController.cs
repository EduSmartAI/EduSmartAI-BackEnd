using Course.Application.Majors.Commands.CreateMajor;
using Course.Application.Subjects.Commands.CreateSubject;

namespace Course.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class SyllabusController(ISender sender) : ControllerBase
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		[HttpPost("[action]")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Tạo mới chuyên ngành",
			Description = "Tạo mới chuyên ngành. Cần xác thực Bearer."
		)]
		public async Task<CreateMajorResponse> CreateMajorProcess([FromBody] CreateMajorCommand request)
		{
			return await ApiControllerHelper.HandleRequest<CreateMajorCommand, CreateMajorResponse, bool>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new CreateMajorResponse()
			);
		}

		[HttpPost("[action]")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Tạo mới môn học",
			Description = "Tạo mới môn học. Cần xác thực Bearer."
		)]
		public async Task<CreateSubjectResponse> CreateSubjectProcess([FromBody] CreateSubjectCommand request)
		{
			return await ApiControllerHelper.HandleRequest<CreateSubjectCommand, CreateSubjectResponse, bool>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new CreateSubjectResponse()
			);
		}
	}
}
