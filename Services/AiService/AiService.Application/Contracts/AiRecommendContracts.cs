using System.Text.Json.Serialization;

namespace AiService.Application.Contracts
{
    public class AiRecommendContracts
    {
        public record Major(string MajorCode, string MajorName, string Description);
        public record SearchHit(string MajorCode, string MajorName, string Snippet, float? Similarity);
        public sealed class MajorEvaluation
        {
            [JsonPropertyName("major_code")]
            public string MajorCode { get; set; } = "";

            [JsonPropertyName("major_name")]
            public string MajorName { get; set; } = "";

            [JsonPropertyName("supports")]
            public bool Supports { get; set; }

            [JsonPropertyName("support_score")]
            public int SupportScore { get; set; }

            [JsonPropertyName("reasons")]
            public string Reasons { get; set; } = "";
        }
        public sealed class ExternalSuggestion
        {
            [JsonPropertyName("major_code")]
            public string MajorCode { get; set; } = "";

            [JsonPropertyName("major_name")]
            public string MajorName { get; set; } = "";

            [JsonPropertyName("description")]
            public string Description { get; set; } = "";

            [JsonPropertyName("why_for_you")]
            public string WhyForYou { get; set; } = "";
        }
        public sealed class ProposedMajor
        {
            public string MajorCode { get; set; } = "";
            public string MajorName { get; set; } = "";
            public string Description { get; set; } = "";
        }
        public sealed class EvaluateResult
        {
            public object Inputs { get; set; } = default!;
            public List<MajorEvaluation> Evaluations { get; set; } = new();
            public List<MajorEvaluation> Matched { get; set; } = new();
            public List<ExternalSuggestion> ExternalSuggestions { get; set; } = new();
            public ProposedMajor? ProposedMajor { get; set; }
        }
    }
}
