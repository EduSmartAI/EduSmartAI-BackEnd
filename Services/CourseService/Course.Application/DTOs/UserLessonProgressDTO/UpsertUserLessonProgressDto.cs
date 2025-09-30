namespace Course.Application.DTOs.UserLessonProgressDTO
{
	public record UpsertUserLessonProgressDto(
		short? Status,            // 0/1/2; null = không đổi
		int? LastPositionSec,     // null = không đổi
		int? WatchedDeltaSec      // null = 0
	);
}
