using BuildingBlocks.Messaging.Events.CourseService.ModuleQuizScoresSelectEvents;
using MassTransit;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.QuizCourses.Consumers
{
	public class GetLatestModuleQuizScoresConsumer(IQuizCourseService _quizCourseService) : IConsumer<GetLatestModuleQuizScoresEvent>
	{
		public async Task Consume(ConsumeContext<GetLatestModuleQuizScoresEvent> context)
		{
			var evt = context.Message;
			var response = await _quizCourseService.GetLatestModuleQuizScoresAsync(evt, context.CancellationToken);
			await context.RespondAsync(response);
		}
	}
}
