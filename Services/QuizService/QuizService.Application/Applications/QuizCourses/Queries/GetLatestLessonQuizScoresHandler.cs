using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.CourseService.LessonQuizScoresSelectEvents;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.QuizCourses.Queries
{
	public class GetLatestLessonQuizScoresHandler(IQuizCourseService _quizCourseService) : IQueryHandler<GetLatestLessonQuizScoresQuery, GetLatestLessonQuizScoresResponseEvent>
	{
		public async Task<GetLatestLessonQuizScoresResponseEvent> Handle(GetLatestLessonQuizScoresQuery request, CancellationToken cancellationToken)
		{
			var quizRequest = new GetLatestLessonQuizScoresEvent(
				request.StudentId,
				request.CourseId,
				request.LessonIds);
			return await _quizCourseService.GetLatestLessonQuizScoresAsync(quizRequest, cancellationToken);
		}
	}
}
