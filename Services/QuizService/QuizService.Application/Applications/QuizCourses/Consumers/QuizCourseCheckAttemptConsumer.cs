using BuildingBlocks.Messaging.Events.CourseService.QuizCourseCheckAttemptEvents;
using MassTransit;
using QuizService.Application.Applications.QuizCourses.Commands;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.QuizCourses.Consumers
{
	public class QuizCourseCheckAttemptConsumer(IQuizCourseService _quizCourseService) : IConsumer<QuizCourseCheckAttemptEvent>
	{
		public async Task Consume(ConsumeContext<QuizCourseCheckAttemptEvent> context)
		{
			var evt = context.Message;

			var request = new QuizCourseCheckAttemptCommand(evt.QuizId, evt.StudentId);

			var response = await _quizCourseService.CheckStudentQuizAttemptAsync(request, context.CancellationToken);

			var eventResponse = new QuizCourseCheckAttemptEventResponse
			{
				Success = response.Success,
				Message = response.Message,
				Response = response.Response,
				DetailErrors = response.DetailErrors,
				MessageId = response.MessageId
			};

			await context.RespondAsync(eventResponse);
		}
	}
}
