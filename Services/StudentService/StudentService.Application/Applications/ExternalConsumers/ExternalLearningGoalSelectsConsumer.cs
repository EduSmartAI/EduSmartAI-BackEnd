using BuildingBlocks.Messaging.Events.QuizService.LearningGoalSelectsEvents;
using MassTransit;
using MediatR;
using StudentService.Application.Applications.LearningGoals.Queries;

namespace StudentService.Application.Applications.ExternalConsumers;

public class ExternalLearningGoalSelectsConsumer(IMediator mediator) : IConsumer<LearningGoalSelectsEvent>
{
    /// <summary>
    /// Handles the LearningGoalSelectsEvent by sending an ExternalLearningGoalSelectsQuery through MediatR
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<LearningGoalSelectsEvent> context)
    {
        var evt = context.Message;
        
        var command = new LearningGoalSelectsQuery();
        
        var response = await mediator.Send(command);
        
        await context.RespondAsync(response);
    }
}