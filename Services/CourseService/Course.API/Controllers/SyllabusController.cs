using Course.Application.Majors.Commands.CreateMajor;

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
	}
}
