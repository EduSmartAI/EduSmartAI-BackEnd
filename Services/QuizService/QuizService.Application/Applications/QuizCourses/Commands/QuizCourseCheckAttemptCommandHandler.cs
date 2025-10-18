using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.QuizCourses.Commands
{
	public class QuizCourseCheckAttemptCommandHandler(IQuizCourseService _quizCourseService) : ICommandHandler<QuizCourseCheckAttemptCommand, QuizCourseCheckAttemptResponse>
	{
		public async Task<QuizCourseCheckAttemptResponse> Handle(QuizCourseCheckAttemptCommand request, CancellationToken cancellationToken)
		{
			return await _quizCourseService.CheckStudentQuizAttemptAsync(request, cancellationToken);
		}
	}
}
