namespace Course.Application.DTOs
{
	public record LessonDetailDto(
		Guid LessonId,
		string Title,
		string VideoUrl,
		int? VideoDurationSec,
		int PositionIndex,
		bool IsActive
	);
}
