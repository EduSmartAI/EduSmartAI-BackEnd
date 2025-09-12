namespace Course.Application.DTOs
{
	public record UpdateModuleDto(
		Guid? ModuleId,
		string ModuleName,
		string? Description,
		int PositionIndex,
		bool IsActive,
		bool IsCore,
		int? DurationMinutes,
		short? Level,
		List<UpdateModuleObjectiveDto>? Objectives,
		List<UpdateLessonDto> Lessons
	);

	public record UpdateModuleObjectiveDto(Guid? ObjectiveId, string Content, int PositionIndex, bool IsActive);
}
