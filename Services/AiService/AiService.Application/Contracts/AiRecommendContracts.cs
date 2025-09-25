using System.Text.Json;
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
        public sealed class EvaluateResult
        {
            public object Inputs { get; set; } = default!;
            public List<MajorEvaluation> Evaluations { get; set; } = new();
            public List<MajorEvaluation> Matched { get; set; } = new();
            public List<ExternalSuggestion> ExternalSuggestions { get; set; } = new();
        }
        public sealed class DocumentDto
        {
            public string DocId { get; set; } = "";
            public string Content { get; set; } = "";
            public JsonElement Metadata { get; set; }
            public double Score { get; set; }
        }
        public sealed class SearchRequest
        {
            public string Query { get; set; } = "";
            public int K { get; set; } = 40;
        }
        public sealed class AskRequest
        {
            public string Question { get; set; } = "";
            public int K { get; set; } = 40;
            public bool ShowSources { get; set; } = false;
        }
        public sealed class AskResponse
        {
            public string Answer { get; set; } = "";
            public RoadmapPayload? Roadmap { get; set; }
        }

        public sealed class RoadmapPayload
        {
            [JsonPropertyName("roadmap_title")] public string RoadmapTitle { get; set; } = string.Empty;
            [JsonPropertyName("steps")] public List<RoadmapStepPayload> Steps { get; set; } = new();
        }
        public sealed class RoadmapStepPayload
        {
            [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
            [JsonPropertyName("duration_weeks")] public int DurationWeeks { get; set; }
            [JsonPropertyName("objectives")] public List<string> Objectives { get; set; } = new();
            [JsonPropertyName("suggested_courses")] public List<SuggestedCoursePayload> SuggestedCourses { get; set; } = new();
        }
        public sealed class SuggestedCoursePayload
        {
            [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
            [JsonPropertyName("link")] public string Link { get; set; } = string.Empty;
            [JsonPropertyName("provider")] public string Provider { get; set; } = string.Empty;
            [JsonPropertyName("reason")] public string Reason { get; set; } = string.Empty;
            [JsonPropertyName("level")] public string Level { get; set; } = "—";
            [JsonPropertyName("rating")] public string Rating { get; set; } = "—";
        }
    }
}
