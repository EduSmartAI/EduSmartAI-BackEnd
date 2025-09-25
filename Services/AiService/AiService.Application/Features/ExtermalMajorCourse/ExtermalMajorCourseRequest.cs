using MediatR;

namespace AiService.Application.Features.ExtermalMajorCourse
{
    public class ExtermalMajorCourseRequest : IRequest<ExtermalMajorCourseResponse>
    {
        public string GoalMajor { get; set; } = string.Empty;
        public Guid LearningPathId { get; set; }
    }
}
