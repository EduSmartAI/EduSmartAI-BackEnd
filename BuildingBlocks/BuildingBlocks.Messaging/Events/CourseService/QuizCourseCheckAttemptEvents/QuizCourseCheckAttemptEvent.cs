namespace BuildingBlocks.Messaging.Events.CourseService.QuizCourseCheckAttemptEvents
{
	public record QuizCourseCheckAttemptEvent(
		Guid QuizId,
		Guid StudentId
	);
}
