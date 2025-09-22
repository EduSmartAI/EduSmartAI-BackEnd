namespace BuildingBlocks.Messaging.Events.QuizService.QuizzesCourseInsertEvents
{
	public record ModuleQuizInsertEvent(
		Guid CorrelationId,
		Guid CourseId,
		Guid ModuleId,
		CreateQuizPayload Quiz
	);
}
