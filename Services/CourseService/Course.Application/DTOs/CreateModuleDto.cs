namespace Course.Application.DTOs
{
	public record CreateModuleDto(
		string ModuleName,
		string? Description,
		int PositionIndex,
		bool IsActive,
		bool IsCore,                      // new
		int? DurationMinutes,             // new
		short? Level,                     // new
		List<CreateModuleObjectiveDto>? Objectives,  // optional
		List<CreateLessonDto> Lessons
	);

	public record CreateModuleObjectiveDto(string Content, int PositionIndex = 0, bool IsActive = true);
}
