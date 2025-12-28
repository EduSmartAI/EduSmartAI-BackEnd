using BuildingBlocks.Messaging.Events.QuizService;
using Course.Application.Semesters.Queries.SelectSemesters;

namespace Course.Application.Consumers;

public class SemesterSelectsConsumer(IMediator mediator) : IConsumer<SemesterSelectsEvent>
{
    public async Task Consume(ConsumeContext<SemesterSelectsEvent> context)
    {
        var evt = context.Message;
        
        var command = new SemesterSelectsQuery();
        
        var response = await mediator.Send(command);
        
        await context.RespondAsync(response);
    }
}