using MediatR;
using System.Text.Json.Serialization;

namespace AiService.Application.Features.AiExternalCourse
{
    public class AiExternalCourseRequest : IRequest<AiExternalCourseResponse>
    {
        [JsonPropertyName("goal_major")] public string GoalMajor { get; set; } = string.Empty;
        public string LearningPathId { get; set; } = string.Empty;
        public string CurrentUserEmail { get; set; } = string.Empty;
        public string MajorCode { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
