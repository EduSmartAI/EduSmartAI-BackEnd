namespace BuildingBlocks.Messaging.Events.CourseService
{
	public record CourseCompletedEvent
	{
		public Guid UserId { get; init; }
		public Guid CourseId { get; init; }
		public DateTime CompletedAt { get; init; }
	}
}
