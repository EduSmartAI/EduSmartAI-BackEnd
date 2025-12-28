namespace BuildingBlocks.Messaging.Events.CourseService.AITranscriptEvents
{
	public sealed record TranscribeBatchRequested(
		Guid CorrelationId,
		string RequestedBy,                    // email
		string Language,                       // "vi" default
		IReadOnlyList<TranscribeItem> Items,   // each lesson
		DateTime RequestedAtUtc
	);

	public sealed record TranscribeItem(Guid LessonId, string VideoUrl, double? DurationSec);

	// Kết quả cho từng lesson
	public sealed record TranscriptUpsertEvent(
		Guid CorrelationId,
		Guid LessonId,
		string Language,
		short Status,          // "succeeded" | "failed"
		string? TextFull,
		string? VttUrl,
		string? VttPublicId,
		string? Error,
		DateTime ProcessedAtUtc
	);
}
