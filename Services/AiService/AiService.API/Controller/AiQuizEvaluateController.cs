using AiService.Application.DTOs;
using AiService.Application.Handler.Quizzes.Commands;
using BaseService.API.BaseControllers;
using BuildingBlocks.Messaging.Events.QuizService;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace AiService.API.Controller
{
	[Route("api/[controller]")]
	[ApiController]
	public class AiQuizEvaluateController(ISender _sender) : ControllerBase
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Gọi Groq AI để đánh giá kết quả quiz của sinh viên
		/// </summary>
		/// <param name="request">QuizEvaluableCreatedEvent</param>
		/// <returns>Đánh giá chi tiết từ mô hình AI</returns>
		[HttpPost("evaluate")]
		public async Task<QuizEvaluateResponse> EvaluateQuizAsync([FromBody] QuizEvaluableCreatedEvent request)
		{
			var command = new QuizEvaluateCommand(request);
			return await ApiControllerHelper.HandleRequest<QuizEvaluateCommand, QuizEvaluateResponse, AiEvaluationDto>(
			command,
			_logger,
			ModelState,
			async () => await _sender.Send(command),
			new QuizEvaluateResponse());
		}
	}
}
