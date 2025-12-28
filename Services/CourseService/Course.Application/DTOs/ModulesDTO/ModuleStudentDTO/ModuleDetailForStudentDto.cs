using Course.Application.DTOs.LessonsDTO.LessonStudentDTO;
using Course.Application.DTOs.ModulesDTO.ModuleDiscussionDTO;
using Course.Application.DTOs.ModulesDTO.ModuleMaterialDTO;
using Course.Application.DTOs.QuizDTO;

namespace Course.Application.DTOs.ModulesDTO.ModuleStudentDTO
{
	public record ModuleDetailForStudentDto(
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
		List<StudentLessonDetailDto> Lessons,
		QuizOutDto? ModuleQuiz,
		bool CanAttempt,
		Guid? StudentQuizResultId,
		ModuleProgressDto Progress
	);
}
