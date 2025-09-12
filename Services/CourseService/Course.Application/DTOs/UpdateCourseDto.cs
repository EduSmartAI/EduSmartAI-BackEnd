namespace Course.Application.DTOs
{
	public record UpdateCourseDto(
		Guid TeacherId,
		Guid SubjectId,
		string Title,
		string? ShortDescription,
		string? Description,
		string? Slug,
		string? CourseImageUrl,
		//string Status,
		int? DurationMinutes,
		short? Level,
		decimal Price,
		decimal? DealPrice,
		bool IsActive,
		List<UpdateCourseObjectiveDto>? Objectives,
		List<UpdateCourseRequirementDto>? Requirements,
		List<UpdateModuleDto> Modules
	);

	public record UpdateCourseObjectiveDto(Guid? ObjectiveId, string Content, int PositionIndex, bool IsActive);
	public record UpdateCourseRequirementDto(Guid? RequirementId, string Content, int PositionIndex, bool IsActive);
}
