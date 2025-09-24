namespace Course.Application.DTOs.CoursesDTO
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
		List<UpdateCourseAudienceDto>? Audiences,
		List<UpdateCourseTagDto>? CourseTags
	//List<UpdateModuleDto> Modules
	);

	public record UpdateCourseObjectiveDto(Guid? ObjectiveId, string Content, int PositionIndex, bool IsActive);
	public record UpdateCourseRequirementDto(Guid? RequirementId, string Content, int PositionIndex, bool IsActive);
	public record UpdateCourseAudienceDto(Guid? AudienceId, string Content, int PositionIndex, bool IsActive);
	public record UpdateCourseTagDto(long TagId);
}
