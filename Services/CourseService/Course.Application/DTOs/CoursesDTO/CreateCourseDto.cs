using Course.Application.DTOs.ModulesDTO;

namespace Course.Application.DTOs.CoursesDTO
{
	public record CreateCourseDto(
		Guid TeacherId,
		Guid SubjectId,
		string Title,
		string? ShortDescription,
		string? Description,
		string? Slug,
		string? CourseImageUrl,
		int? DurationMinutes,
		short? Level,
		decimal Price,
		decimal? DealPrice,
		string? CourseIntroVideoUrl,
		bool IsActive,
		List<CreateCourseObjectiveDto>? Objectives,
		List<CreateCourseRequirementDto>? Requirements,
		List<CreateCourseTagDto>? CourseTags,
		List<CreateModuleDto> Modules
	);

	public record CreateCourseObjectiveDto(string Content, int PositionIndex = 0, bool IsActive = true);
	public record CreateCourseRequirementDto(string Content, int PositionIndex = 0, bool IsActive = true);
	public record CreateCourseTagDto(long TagId);
}
