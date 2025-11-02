using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.CourseService.ModuleQuizScoresSelectEvents;

namespace QuizService.Application.Applications.QuizCourses.Queries
{
	public record GetLatestModuleQuizScoresQuery(Guid StudentId, Guid CourseId, List<Guid> ModuleIds) : IQuery<GetLatestModuleQuizScoresResponseEvent>;
}
