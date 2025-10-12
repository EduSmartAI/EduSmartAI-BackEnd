using BaseService.API.BaseControllers;
using BuildingBlocks.Messaging.Events.AIService.AiEvaluationUpsertEvents;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NLog;
using StudentService.Application.Applications.AiQuizEvaluates.Commands.CreateAiQuizEvaluate;

namespace StudentService.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AiQuizEvaluateController(ISender sender) : ControllerBase
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		[HttpPost]
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
	}
}
