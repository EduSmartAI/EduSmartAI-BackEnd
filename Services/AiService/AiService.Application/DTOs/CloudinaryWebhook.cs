using System.Text.Json.Serialization;

namespace AiService.Application.DTOs
{
	public record CloudinaryWebhook(
		[property: JsonPropertyName("public_id")] string? PublicId,
		[property: JsonPropertyName("secure_url")] string? SecureUrl,
		[property: JsonPropertyName("url")] string? Url,
		[property: JsonPropertyName("duration")] double? Duration,
		WebhookContext? Context);

	public record WebhookContext(string? lessonId);
}
