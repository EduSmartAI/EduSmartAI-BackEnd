namespace BuildingBlocks.Messaging.Events.CourseService.ModuleQuizScoresSelectEvents
{
	public sealed record GetLatestModuleQuizScoresEvent(
		Guid StudentId,
		Guid CourseId,
		IReadOnlyList<Guid> ModuleIds);
}
