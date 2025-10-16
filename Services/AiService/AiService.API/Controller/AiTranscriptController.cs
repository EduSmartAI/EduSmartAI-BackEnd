using AiService.Application.DTOs;
using AiService.Application.Handler.Transcripts.Commands.CreateTranscript;
using BaseService.API.BaseControllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace AiService.API.Controller
{
	[Route("api/[controller]")]
	[ApiController]
	public class AiTranscriptController(ISender sender) : ControllerBase
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		[HttpPost]
		public async Task<CreateTranscriptResponse> PostCreateTranscript([FromBody] CreateTranscriptCommand request)
		{
			return await ApiControllerHelper.HandleRequest<CreateTranscriptCommand, CreateTranscriptResponse, TranscriptResult>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new CreateTranscriptResponse()
			);
		}
	}
}
