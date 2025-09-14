namespace Course.Application.DTOs.ModulesDTO
{
	public record ModuleDetailDto<TLesson>(
		Guid ModuleId,
		string ModuleName,
		string? Description,
		int PositionIndex,
		bool IsActive,
		bool IsCore,
		int? DurationMinutes,
		decimal? DurationHours,
		short? Level,
		List<ModuleObjectiveDto> Objectives,
		List<TLesson> Lessons
	);

	public record ModuleObjectiveDto(Guid ObjectiveId, string Content, int PositionIndex, bool IsActive);

}
