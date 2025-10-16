namespace AiService.Application.DTOs
{
	public record CreateTranscriptionReq
	{
		public string LessonId { get; init; } = default!;
		public string? VideoUrl { get; init; }
		public string Language { get; init; } = "vi";
		public double? DurationSec { get; init; }
	}
	public class TranscriptResult
	{
		public string LessonId { get; set; } = default!;
		public string Status { get; set; } = "queued";
		public string? Language { get; set; }
		public string? Text { get; set; }
		public List<Segment>? Segments { get; set; }
		public List<Word>? Words { get; set; }
		public string? Error { get; set; }
		public string? VttUrl { get; set; }
		public string? VttPublicId { get; set; }
	}

	public class Segment { public double Start { get; set; } public double End { get; set; } public string Text { get; set; } = ""; }
	public class Word { public double Start { get; set; } public double End { get; set; } public string Text { get; set; } = ""; }
}
