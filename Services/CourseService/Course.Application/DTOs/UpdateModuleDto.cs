namespace Course.Application.DTOs
{
	public record UpdateModuleDto(
		Guid? ModuleId,              // null => module mới
		string ModuleName,
		string? Description,
		int PositionIndex,
		bool IsActive,
		List<UpdateLessonDto> Lessons
	);
}
