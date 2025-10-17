using AiService.Infrastructure.Helpers.TranscriptHelpers;
using System.Text.Json.Serialization;

namespace AiService.Application.DTOs
{
	public class GroqVerboseJson
	{
		[JsonPropertyName("text")] public string? Text { get; set; }
		[JsonPropertyName("segments")] public List<GroqSeg>? Segments { get; set; }
		[JsonPropertyName("words")] public List<GroqWord>? Words { get; set; }

		public class GroqSeg
		{
			[JsonPropertyName("start"), JsonConverter(typeof(NumberOrStringDoubleConverter))]
			public double Start { get; set; }

			[JsonPropertyName("end"), JsonConverter(typeof(NumberOrStringDoubleConverter))]
			public double End { get; set; }

			[JsonPropertyName("text")] public string Text { get; set; } = "";

			[JsonPropertyName("words")] public List<GroqWord>? Words { get; set; }
		}

		public class GroqWord
		{
			[JsonPropertyName("start"), JsonConverter(typeof(NumberOrStringDoubleConverter))]
			public double Start { get; set; }

			[JsonPropertyName("end"), JsonConverter(typeof(NumberOrStringDoubleConverter))]
			public double End { get; set; }

			[JsonPropertyName("word")] public string Word { get; set; } = "";
		}
	}
}
