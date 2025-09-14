namespace Course.Application.DTOs.LessonsDTO
{
	public record LectureLessonDetailDto(
		Guid LessonId,
		string Title,
		string VideoUrl,
		int? VideoDurationSec,
		int PositionIndex,
		bool IsActive
	);
}
