namespace Course.Application.DTOs.UserLessonProgressDTO
{
	public record UpsertUserLessonProgressDto(
		int? LastPositionSec,     // null = không đổi
		int? WatchedDeltaSec      // null = 0
	);
}
