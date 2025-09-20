using Course.Application.DTOs.LessonsDTO;

namespace Course.Application.DTOs.ModulesDTO
{
	public record CreateModuleDto(
		string ModuleName,
		string? Description,
		int PositionIndex,
		bool IsActive,
		bool IsCore,                      // new
		int? DurationMinutes,             // new
		short? Level,                     // new
		List<CreateModuleObjectiveDto>? Objectives,  // optional
		List<CreateLessonDto> Lessons,
		List<CreateModuleDiscussionDto>? Discussions,
		List<CreateModuleMaterialDto>? Materials
	);
}
