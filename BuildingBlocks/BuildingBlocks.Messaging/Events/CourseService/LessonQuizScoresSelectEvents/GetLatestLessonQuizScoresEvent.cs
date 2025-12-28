namespace BuildingBlocks.Messaging.Events.CourseService.LessonQuizScoresSelectEvents
{
	public sealed record GetLatestLessonQuizScoresEvent(
		Guid StudentId,
		Guid CourseId,
		IReadOnlyList<Guid> LessonIds);
}
