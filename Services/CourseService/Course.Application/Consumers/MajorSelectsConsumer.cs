using BuildingBlocks.Messaging.Events.QuizService.MajorSelectsEvents;
using Course.Application.Majors.Queries;
using MassTransit;
using MediatR;

namespace Course.Application.Consumers;

public class MajorSelectsConsumer(IMediator mediator) : IConsumer<MajorSelectsEvent>
{
    public async Task Consume(ConsumeContext<MajorSelectsEvent> context)
    {
        var evt = context.Message;
        
        var command = new MajorSelectsQuery();
        
        var response = await mediator.Send(command);
        
        await context.RespondAsync(response);
    }
}