namespace Course.Application.DTOs
{
	public record CreateModuleDto(
		string ModuleName,
		string? Description,
		int PositionIndex,
		bool IsActive,
		List<CreateLessonDto> Lessons
	);
}
