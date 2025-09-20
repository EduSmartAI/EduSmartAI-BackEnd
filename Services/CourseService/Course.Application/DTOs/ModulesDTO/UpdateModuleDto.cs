using Course.Application.DTOs.LessonsDTO;

namespace Course.Application.DTOs.ModulesDTO
{
	public record UpdateModuleDto(
		Guid? ModuleId,
		string ModuleName,
		string? Description,
		int PositionIndex,
		bool IsActive,
		bool IsCore,
		int? DurationMinutes,
		short? Level,
		List<UpdateModuleObjectiveDto>? Objectives,
		List<UpdateLessonDto> Lessons
	);
}
