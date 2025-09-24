using BuildingBlocks.Messaging.Events.QuizService.StudentTechnologyOrientationEvents;
using MediatR;

namespace AiService.Application.Consumers.StudentMajorRecommends;

public class StudentMajorRecommendRequest : IRequest<StudentMajorOrientationEventResponse>
{
    public string LearningGoal { get; set; } = null!;
    
    public List<string> Frameworks { get; set; } = null!;
    
    public List<string> Languages { get; set; } = null!;
}