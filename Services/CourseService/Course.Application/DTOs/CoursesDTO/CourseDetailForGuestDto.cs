using Course.Application.DTOs.LessonsDTO;
using Course.Application.DTOs.ModulesDTO;

namespace Course.Application.DTOs.CoursesDTO
{
	public record CourseDetailForGuestDto(
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
		List<ModuleDetailDto<GuestLessonDetailDto>> Modules,
		List<CourseCommentDto> Comments,
		List<CourseTagDto> Tags,
		List<CourseRatingDto> Ratings,
		int RatingsCount,
		double RatingsAverage
	);

	public record CourseObjectiveDto(Guid ObjectiveId, string Content, int PositionIndex, bool IsActive);
	public record CourseRequirementDto(Guid RequirementId, string Content, int PositionIndex, bool IsActive);

}
