using Course.Application.DTOs.LessonsDTO;
using Course.Application.DTOs.QuizDTO;

namespace Course.Application.DTOs.ModulesDTO
{
	public record CreateModuleDto(
		string ModuleName,
		string? Description,
		int PositionIndex,
		bool IsActive,
		bool IsCore,
		int? DurationMinutes,
		short? Level,
		List<CreateModuleObjectiveDto>? Objectives,
		List<CreateLessonDto> Lessons,
		List<CreateModuleDiscussionDto>? Discussions,
		List<CreateModuleMaterialDto>? Materials,
		CreateQuizDto? ModuleQuiz
	);
}
