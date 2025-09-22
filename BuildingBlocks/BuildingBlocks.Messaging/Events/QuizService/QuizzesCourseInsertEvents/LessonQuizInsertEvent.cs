namespace BuildingBlocks.Messaging.Events.QuizService.QuizzesCourseInsertEvents
{
	public record LessonQuizInsertEvent(
		Guid CorrelationId,
		Guid CourseId,
		Guid ModuleId,
		Guid LessonId,
		CreateQuizPayload Quiz
	);
}
