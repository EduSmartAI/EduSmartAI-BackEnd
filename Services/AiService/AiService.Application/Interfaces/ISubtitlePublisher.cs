namespace AiService.Application.Interfaces
{
	public interface ISubtitlePublisher
	{
		Task<(string? url, string? publicId)> UploadVttAsync(string lessonId, string vtt, CancellationToken ct);
	}
}
