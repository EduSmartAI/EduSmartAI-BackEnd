using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.CourseService.LessonQuizScoresSelectEvents;

namespace QuizService.Application.Applications.QuizCourses.Queries
{
	public sealed record GetLatestLessonQuizScoresQuery(Guid StudentId, Guid CourseId, IReadOnlyList<Guid> LessonIds) : IQuery<GetLatestLessonQuizScoresResponseEvent>;
}
