using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.CourseService.ModuleQuizScoresSelectEvents;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.QuizCourses.Queries
{
	public class GetLatestModuleQuizScoresHandler(IQuizCourseService quizCourseService) : IQueryHandler<GetLatestModuleQuizScoresQuery, GetLatestModuleQuizScoresResponseEvent>
	{
		public async Task<GetLatestModuleQuizScoresResponseEvent> Handle(GetLatestModuleQuizScoresQuery request, CancellationToken cancellationToken)
		{
			var quizRequest = new GetLatestModuleQuizScoresEvent(
				request.StudentId,
				request.CourseId,
				request.ModuleIds);
			return await quizCourseService.GetLatestModuleQuizScoresAsync(quizRequest, cancellationToken);
		}
	}
}
