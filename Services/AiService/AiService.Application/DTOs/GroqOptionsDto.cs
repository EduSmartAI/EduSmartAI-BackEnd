namespace AiService.Application.DTOs
{
	public sealed class GroqOptionsDto
	{
		public string ApiKey { get; set; } = "";
		public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1/";
		public string Model { get; set; } = "llama-3.3-70b-versatile";
		public string RubricVersion { get; set; } = "quiz-v2.1";
	}
}
