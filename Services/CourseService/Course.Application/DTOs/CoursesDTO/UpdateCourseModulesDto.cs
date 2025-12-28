namespace Course.Application.DTOs.CoursesDTO
{
	/// <summary>
	/// DTO for updating multiple modules in a course
	/// </summary>
	public record UpdateCourseModulesDto(
		List<UpdateCourseModuleDto> Modules
	);

	/// <summary>
	/// DTO for updating a single module within course context
	/// </summary>
	public record UpdateCourseModuleDto(
		Guid? ModuleId,              // null => create new module
		string ModuleName,
		string? Description,
		int PositionIndex,
		bool IsActive,
		bool IsCore,
		int? DurationMinutes,
		short? Level,
		List<UpdateCourseModuleObjectiveDto>? Objectives,
		List<UpdateCourseLessonDto> Lessons,
		List<UpdateModuleDiscussionDto>? Discussions,
		List<UpdateModuleMaterialDto>? Materials
	);

	/// <summary>
	/// DTO for updating module objective within course context
	/// </summary>
	public record UpdateCourseModuleObjectiveDto(
		Guid? ObjectiveId,           // null => create new objective
		string Content,
		int PositionIndex,
		bool IsActive
	);

	/// <summary>
	/// DTO for updating lesson within course context
	/// </summary>
	public record UpdateCourseLessonDto(
		Guid? LessonId,              // null => create new lesson
		string Title,
		string VideoUrl,
		int? VideoDurationSec,
		int PositionIndex,
		bool IsActive
	);

	public record UpdateModuleDiscussionDto(
		Guid? DiscussionId,          // null => create new discussion
		string Title,
		string Description,
		string DiscussionQuestion,
		bool IsActive
	);

	public record UpdateModuleMaterialDto(
		Guid? MaterialId,            // null => create new material
		string Title,
		string Description,
		string FileUrl,
		bool IsActive
	);
}
