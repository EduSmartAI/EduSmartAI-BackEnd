using BaseService.API.BaseControllers;
using BuildingBlocks.Messaging.Events.AIService.AiEvaluationUpsertEvents;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using StudentService.Application.Applications.AiQuizEvaluates.Commands.CreateAiQuizEvaluate;
using StudentService.Application.Applications.Dashboards.Queries;

namespace StudentService.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AiQuizEvaluateController(ISender sender) : ControllerBase
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		[HttpPost]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		public async Task<CreateAiQuizEvaluateResponse> CreateAiQuizEvaluateProcess([FromBody] AiEvaluationUpsertEvent request)
		{
			var command = new CreateAiQuizEvaluateCommand(request);

			return await ApiControllerHelper.HandleRequest<CreateAiQuizEvaluateCommand, CreateAiQuizEvaluateResponse, string>(
				command,
				_logger,
				ModelState,
				async () => await sender.Send(command),
				new CreateAiQuizEvaluateResponse()
			);
		}

		[HttpGet]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		public async Task<GetLatestModuleAiEvaluationsResponse> GetModuleDashboardResponse([FromQuery] GetLatestModuleAiEvaluationsQuery request)
		{
			return await ApiControllerHelper.HandleRequest<GetLatestModuleAiEvaluationsQuery, GetLatestModuleAiEvaluationsResponse, GetLatestModuleAiEvaluationsPayload>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new GetLatestModuleAiEvaluationsResponse()
			);
		}

		[HttpGet("[action]")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		public async Task<GetLatestLessonAiEvaluationsResponse> GetLatestLessonAiEvaluationsAsync([FromQuery] GetLatestLessonAiEvaluationsQuery request)
		{
			return await ApiControllerHelper.HandleRequest<GetLatestLessonAiEvaluationsQuery, GetLatestLessonAiEvaluationsResponse, GetLatestLessonAiEvaluationsPayload>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new GetLatestLessonAiEvaluationsResponse()
			);
		}
	}
}
