using MassTransit;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.QuizCourses.Consumers
{
	public class GetLatestLessonQuizScoresConsumer(IQuizCourseService _quizCourseService) : IConsumer<BuildingBlocks.Messaging.Events.CourseService.LessonQuizScoresSelectEvents.GetLatestLessonQuizScoresEvent>
	{
		public async Task Consume(ConsumeContext<BuildingBlocks.Messaging.Events.CourseService.LessonQuizScoresSelectEvents.GetLatestLessonQuizScoresEvent> context)
		{
			var evt = context.Message;
			var response = await _quizCourseService.GetLatestLessonQuizScoresAsync(evt, context.CancellationToken);
			await context.RespondAsync(response);
		}
	}
}
