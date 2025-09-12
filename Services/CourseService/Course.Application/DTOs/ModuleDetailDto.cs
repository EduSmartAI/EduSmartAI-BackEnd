namespace Course.Application.DTOs
{
	public record ModuleDetailDto(
		Guid ModuleId,
		string ModuleName,
		string? Description,
		int PositionIndex,
		bool IsActive,
		bool IsCore,                    // NEW
		int? DurationMinutes,           // NEW
		decimal? DurationHours,         // GENERATED
		short? Level,                   // NEW
		List<ModuleObjectiveDto> Objectives,
		//List<LessonDetailDto> Lessons,
		List<GuestLessonDetailDto> GuestLessons
	);

	public record ModuleObjectiveDto(Guid ObjectiveId, string Content, int PositionIndex, bool IsActive);

}
