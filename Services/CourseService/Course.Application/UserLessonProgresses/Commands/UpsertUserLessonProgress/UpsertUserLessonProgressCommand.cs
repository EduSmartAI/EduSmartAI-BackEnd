using Course.Application.DTOs.UserLessonProgressDTO;

namespace Course.Application.UserLessonProgresses.Commands.CreateUserLessonProgress
{
	public record UpsertUserLessonProgressCommand(Guid LessonId, UpsertUserLessonProgressDto UserLessonProgress) : ICommand<UpsertUserLessonProgressResponse>;

	public record UpsertUserLessonProgressResponse : AbstractApiResponse<UserLessonProgressEntity>
	{
		public override UserLessonProgressEntity Response { get; set; } = default!;
	}

	public record UserLessonProgressEntity(
		Guid LessonId, 
		short Status, 
		int LastPositionSec, 
		int DurationWatchedSec, 
		DateTime? CompletedAt
	);
}
