using Course.Application.DTOs.LessonsDTO;
using Course.Application.DTOs.ModulesDTO.ModuleDiscussionDTO;
using Course.Application.DTOs.ModulesDTO.ModuleMaterialDTO;
using Course.Application.DTOs.QuizDTO;

namespace Course.Application.DTOs.ModulesDTO
{
	public record ModuleDetailForLectureDto(
		Guid ModuleId,
		string ModuleName,
		string? Description,
		int PositionIndex,
		bool IsActive,
		bool IsCore,
		int? DurationMinutes,
		decimal? DurationHours,
		short? Level,
		List<ModuleObjectiveDto> Objectives,
		List<ModuleDiscussionDetailDto> ModuleDiscussionDetails,
		List<ModuleMaterialDetailDto> ModuleMaterialDetails,
		List<LectureLessonDetailDto> Lessons,
		QuizOutDto? ModuleQuiz = null
	);
}
