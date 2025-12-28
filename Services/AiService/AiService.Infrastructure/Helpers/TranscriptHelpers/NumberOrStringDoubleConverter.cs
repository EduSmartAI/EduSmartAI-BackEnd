using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiService.Infrastructure.Helpers.TranscriptHelpers
{
	public sealed class NumberOrStringDoubleConverter : JsonConverter<double>
	{
		public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.Number && reader.TryGetDouble(out var d)) return d;
			if (reader.TokenType == JsonTokenType.String && double.TryParse(reader.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s)) return s;
			return 0d;
		}
		public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options) => writer.WriteNumberValue(value);
	}
}
