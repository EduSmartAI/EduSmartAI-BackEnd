using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.QuizCourses.Commands
{
	public record QuizCourseCheckAttemptCommand(Guid QuizId) : ICommand<QuizCourseCheckAttemptResponse>;

	public record QuizCourseCheckAttemptResponse : AbstractApiResponse<bool>
	{
		public override bool Response { get; set; }
	}
}
