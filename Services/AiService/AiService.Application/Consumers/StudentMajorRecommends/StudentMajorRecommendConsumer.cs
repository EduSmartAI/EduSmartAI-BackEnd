using BuildingBlocks.Messaging.Events.QuizService.StudentTechnologyOrientationEvents;
using MassTransit;
using MediatR;

namespace AiService.Application.Consumers.StudentMajorRecommends;

public class StudentMajorRecommendConsumer(IMediator mediator) : IConsumer<StudentMajorOrientationEvent>
{
    public async Task Consume(ConsumeContext<StudentMajorOrientationEvent> context)
    {
        var evt = context.Message;

        var request = new StudentMajorRecommendRequest
        {
            LearningGoal = evt.LearningGoal,
            Languages = evt.Languages,
            Frameworks = evt.Frameworks
        };

        var response = await mediator.Send(request);

        await context.RespondAsync(response);
    }
}