using System.Text.Json.Serialization;

namespace BuildingBlocks.Messaging.Events.AIService.ExternalInsertMajorEvents;

public class ExternalInsertMajorEvent
{
    [JsonPropertyName("roadmap_title")] public string RoadmapTitle { get; set; } = string.Empty;
    [JsonPropertyName("steps")] public List<RoadmapStepPayloadEvent> Steps { get; set; } = new();
}

public sealed class RoadmapStepPayloadEvent
{
    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
    [JsonPropertyName("duration_weeks")] public int DurationWeeks { get; set; }
    [JsonPropertyName("objectives")] public List<string> Objectives { get; set; } = new();
    [JsonPropertyName("suggested_courses")] public List<SuggestedCoursePayloadEvent> SuggestedCourses { get; set; } = new();
}
public sealed class SuggestedCoursePayloadEvent
{
    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
    [JsonPropertyName("link")] public string Link { get; set; } = string.Empty;
    [JsonPropertyName("provider")] public string Provider { get; set; } = string.Empty;
    [JsonPropertyName("reason")] public string Reason { get; set; } = string.Empty;
}