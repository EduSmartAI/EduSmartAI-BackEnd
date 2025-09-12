namespace Course.Application.DTOs
{
	public record CreateCourseDto(
		Guid TeacherId,
		Guid SubjectId,
		string Title,
		string? ShortDescription,
		string? Description,
		string? Slug,                // optional
		string? CourseImageUrl,      // optional
		int? DurationMinutes,
		short? Level,
		decimal Price,
		decimal? DealPrice,
		bool IsActive,
		List<CreateCourseObjectiveDto>? Objectives,     // optional
		List<CreateCourseRequirementDto>? Requirements, // optional
		List<CreateModuleDto> Modules
	);

	public record CreateCourseObjectiveDto(string Content, int PositionIndex = 0, bool IsActive = true);
	public record CreateCourseRequirementDto(string Content, int PositionIndex = 0, bool IsActive = true);
}
