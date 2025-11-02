using BaseService.API.BaseControllers;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.StudentService.Dashboards.LessonDashboard;
using BuildingBlocks.Messaging.Events.StudentService.Dashboards.ModuleDashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using StudentService.Application.Applications.Dashboards.Queries;
using Swashbuckle.AspNetCore.Annotations;

namespace StudentService.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class StudentDashboardsController(ISender sender) : ControllerBase
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		[HttpGet("[action]")]
		[Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Lấy Module Dashboard",
			Description = "Trả về Module Dashboard theo tham số query. Cần xác thực Bearer."
		)]
		public async Task<GetModuleDashboardEventResponse> GetModuleDashboardProcess([FromQuery] GetModuleDashboardQuery request)
		{
			return await ApiControllerHelper.HandleRequest<GetModuleDashboardQuery, GetModuleDashboardEventResponse, ModuleDashboardContract>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new GetModuleDashboardEventResponse()
			);
		}

		[HttpGet("[action]")]
		[Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(
			Summary = "Lấy Module Dashboard",
			Description = "Trả về Module Dashboard theo tham số query. Cần xác thực Bearer."
		)]
		public async Task<GetLessonDashboardEventResponse> GetLessonDashboardProcess([FromQuery] GetLessonDashboardQuery request)
		{
			return await ApiControllerHelper.HandleRequest<GetLessonDashboardQuery, GetLessonDashboardEventResponse, LessonDashboardContract>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new GetLessonDashboardEventResponse()
			);
		}
	}
}
