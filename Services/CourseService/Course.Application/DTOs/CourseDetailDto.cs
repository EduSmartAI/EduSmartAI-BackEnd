namespace Course.Application.DTOs
{
	public record CourseDetailDto(
		Guid CourseId,
		Guid TeacherId,
		Guid SubjectId,
		string SubjectCode,
		string Title,
		string? ShortDescription,
		string? Description,
		string? Slug,
		string? CourseImageUrl,
		//string Status,
		int LearnerCount,
		int? DurationMinutes,
		decimal? DurationHours,
		short? Level,
		decimal Price,
		decimal? DealPrice,
		bool IsActive,
		DateTime CreatedAt,
		DateTime UpdatedAt,
		List<CourseObjectiveDto> Objectives,
		List<CourseRequirementDto> Requirements,
		List<ModuleDetailDto> Modules
	);

	public record CourseObjectiveDto(Guid ObjectiveId, string Content, int PositionIndex, bool IsActive);
	public record CourseRequirementDto(Guid RequirementId, string Content, int PositionIndex, bool IsActive);

}
