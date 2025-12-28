namespace Course.Application.DTOs.LessonsDTO.LessonStudentDTO
{
	public record ContinueHintDto(
		Guid ModuleId,
		string ModuleName,
		Guid LessonId,
		string LessonTitle,
		int ResumeSecond
	);
}
