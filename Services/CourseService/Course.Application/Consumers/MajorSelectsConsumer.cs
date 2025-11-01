using BuildingBlocks.Messaging.Events.QuizService;
using BuildingBlocks.Messaging.Events.QuizService.MajorSelectsEvents;
using Course.Application.Majors.Queries;

namespace Course.Application.Consumers;

public class MajorSelectsConsumer(IMediator mediator) : IConsumer<MajorCodeSelectsEvent>
{
    public async Task Consume(ConsumeContext<MajorCodeSelectsEvent> context)
    {
        var evt = context.Message;
        
        var command = new MajorCodeSelectsQuery
        {
            MajorCodes = evt.MajorCodes
        };
        
        var response = await mediator.Send(command);
        
        await context.RespondAsync(response);
    }
}