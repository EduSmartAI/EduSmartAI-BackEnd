using Course.Application.Courses.Commands.UpdateModule;

namespace Course.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ModulesController(ISender sender) : ControllerBase
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Update a module within a course, including its objectives and lessons
		/// </summary>
		/// <param name="id"></param>
		/// <param name="request"></param>
		/// <returns></returns>
		[HttpPut("{id:guid}")]
		[Authorize(Roles = ConstRole.Lecturer, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Update a module within a course",
			Description = "Update a module within a course, including its objectives and lessons"
		)]
		public async Task<UpdateModuleResponse> ProcessRequestPutModule([FromRoute] Guid id, [FromBody] UpdateModuleCommand request)
		{
			var response = new UpdateModuleResponse();
			if (request.ModuleId == Guid.Empty)
				request = request with { ModuleId = id };
			else if (request.ModuleId != id)
			{
				response.Success = false;
				response.SetMessage("ModuleId in route and payload do not match.");
				return response;
			}
			return await ApiControllerHelper.HandleRequest<UpdateModuleCommand, UpdateModuleResponse, string>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new UpdateModuleResponse()
			);
		}
	}
}
