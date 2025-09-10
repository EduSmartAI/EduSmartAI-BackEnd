namespace Course.Application.DTOs
{
	public record ModuleDetailDto(
		Guid ModuleId,
		string ModuleName,
		//string? Description,
		int PositionIndex,
		bool IsActive,
		IReadOnlyList<LessonDetailDto> Lessons
	);
}
