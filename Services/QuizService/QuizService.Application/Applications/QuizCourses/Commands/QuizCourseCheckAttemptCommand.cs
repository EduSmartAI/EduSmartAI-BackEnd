using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.CourseService.QuizCourseCheckAttemptEvents;

namespace QuizService.Application.Applications.QuizCourses.Commands
{
	public record QuizCourseCheckAttemptCommand(Guid QuizId, Guid StudentId) : ICommand<QuizCourseCheckAttemptResponse>;

	public record QuizCourseCheckAttemptResponse : AbstractApiResponse<QuizCourseCheckAttemptEntity>
	{
		public override QuizCourseCheckAttemptEntity Response { get; set; }
	}
}
