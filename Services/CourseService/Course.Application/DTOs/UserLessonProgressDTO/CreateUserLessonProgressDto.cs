namespace Course.Application.DTOs.UserLessonProgressDTO
{
	public record CreateUserLessonProgressDto(
		Guid LessonId,
		short Status, // 0 - Not Started, 1 - In Progress, 2 - Completed
		DateTime? CompletedAt,
		int DurationWatchedSec,
		int? LastPositionSec
	);
}
