using Course.Application.DTOs.ModulesDTO;

namespace Course.Application.DTOs.CoursesDTO
{
	public record CourseDetailForLectureDto(
		Guid CourseId,
		Guid TeacherId,
		Guid SubjectId,
		string SubjectCode,
		string Title,
		string? ShortDescription,
		string? Description,
		string? Slug,
		string? CourseImageUrl,
		int LearnerCount,
		string CourseIntroVideoUrl,
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
		List<CourseAudienceDto> Audiences,
		List<CourseTagDto> Tags,
		List<ModuleDetailForLectureDto> Modules,
		List<CourseCommentDto> Comments,
		List<CourseRatingDto> Ratings,
		int RatingsCount,
		double RatingsAverage
	);
}
