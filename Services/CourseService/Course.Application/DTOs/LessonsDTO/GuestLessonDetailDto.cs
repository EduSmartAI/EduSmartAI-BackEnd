namespace Course.Application.DTOs.LessonsDTO
{
	public record GuestLessonDetailDto
	(
		Guid LessonId,
		string Title,
		int PositionIndex,
		bool IsActive
	);
}
