using Course.Application.DTOs.LessonsDTO;
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
		//string Status,
		int LearnerCount,
		string VideoUrl,
		int VideoDurationSec,
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
		List<ModuleDetailDto<LectureLessonDetailDto>> Modules
	);
}
