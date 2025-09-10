namespace Course.Application.DTOs
{
	public record CreateLessonDto(
		string Title,
		string VideoUrl,
		int? VideoDurationSec,
		int PositionIndex,
		bool IsActive
	);
}
