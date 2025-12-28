namespace Course.Application.DTOs.UserLessonProgressDTO
{
	public record UpsertUserLessonProgressDto(
		int? LastSeenPositionSec,     // null = không đổi
		int? WatchedDeltaSec      // null = 0
	);
}
