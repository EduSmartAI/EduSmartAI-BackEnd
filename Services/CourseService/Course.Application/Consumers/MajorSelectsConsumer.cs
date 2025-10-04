using BuildingBlocks.Messaging.Events.QuizService.MajorSelectsEvents;
using Course.Application.Majors.Queries;

namespace Course.Application.Consumers;

public class MajorSelectsConsumer(IMediator mediator) : IConsumer<MajorSelectsEvent>
{
    public async Task Consume(ConsumeContext<MajorSelectsEvent> context)
    {
        var evt = context.Message;
        
        var command = new MajorSelectsQuery
        {
            MajorCodes = evt.MajorCodes
        };
        
        var response = await mediator.Send(command);
        
        await context.RespondAsync(response);
    }
}