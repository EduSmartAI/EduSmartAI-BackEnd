using Course.Application.DTOs.ModulesDTO.ModuleStudentDTO;

namespace Course.Application.DTOs.CoursesDTO.CourseStudentDTO
{
	public record CourseDetailForStudentDto(
		Guid CourseId,
		Guid SubjectId,
		string SubjectCode,
		string Title,
		string? ShortDescription,
		string? Description,
		string? Slug,
		string? CourseImageUrl,
		int LearnerCount,
		//string VideoUrl,
		//int VideoDurationSec,
		int? DurationMinutes,
		decimal? DurationHours,
		short? Level,
		//decimal Price,
		//decimal? DealPrice,
		bool IsActive,
		DateTime CreatedAt,
		DateTime UpdatedAt,
		List<CourseObjectiveDto> Objectives,
		List<CourseRequirementDto> Requirements,
		List<ModuleDetailForStudentDto> Modules,
		List<CourseCommentDto> Comments,
		List<CourseTagDto> Tags,
		List<CourseRatingDto> Ratings,
		int RatingsCount,
		double RatingsAverage,
		CourseProgressDto Progress
	//ContinueHintDto? Continue
	);
}
