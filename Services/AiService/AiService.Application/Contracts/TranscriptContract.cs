namespace AiService.Application.Contracts
{
	public record TranscribeJob(string LessonId, string VideoUrl, string Language = "vi", double? DurationSec = null);
}
