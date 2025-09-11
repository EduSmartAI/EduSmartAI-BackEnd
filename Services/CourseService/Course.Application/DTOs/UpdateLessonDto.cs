namespace Course.Application.DTOs
{
	public record UpdateLessonDto(
		Guid? LessonId,              // null => lesson mới
		string Title,
		string VideoUrl,
		int? VideoDurationSec,
		int PositionIndex,
		bool IsActive
	);
}
