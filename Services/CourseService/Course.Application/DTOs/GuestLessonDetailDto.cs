namespace Course.Application.DTOs
{
	public record GuestLessonDetailDto
	(
		Guid LessonId,
		string Title,
		int PositionIndex,
		bool IsActive
	);
}
