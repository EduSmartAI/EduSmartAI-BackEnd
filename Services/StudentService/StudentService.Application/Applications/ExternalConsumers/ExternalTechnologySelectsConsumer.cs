using BuildingBlocks.Messaging.Events.QuizService.TechnologySelectsEvents;
using MassTransit;
using MediatR;
using StudentService.Application.Applications.LearningGoals.Queries;

namespace StudentService.Application.Applications.ExternalConsumers;

public class ExternalTechnologySelectsConsumer(IMediator mediator) : IConsumer<TechnologySelectsEvent>
{
    public async Task Consume(ConsumeContext<TechnologySelectsEvent> context)
    {
        var evt = context.Message;
        
        var command = new TechnologySelectsQuery();
        
        var response = await mediator.Send(command);
        
        await context.RespondAsync(response);
    }
}