using MediatR;
using System.Text.Json.Serialization;

namespace AiService.Application.Features.AiExternalCourse
{
    public class AiExternalCourseRequest : IRequest<AiExternalCourseResponse>
    {
        [JsonPropertyName("goal_major")] public string GoalMajor { get; set; } = string.Empty;
    }
}
