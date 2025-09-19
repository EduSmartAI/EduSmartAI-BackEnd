using BuildingBlocks.Messaging.Events.QuizService.SubjectSelectEvents;
using Course.Application.Subjects.Queries;
using MassTransit;
using MediatR;

namespace Course.Application.Consumers;

public class SubjectSelectsConsumer(IMediator mediator) : IConsumer<SubjectSelectsEvent>
{
    public async Task Consume(ConsumeContext<SubjectSelectsEvent> context)
    {
        var evt = context.Message;
        
        var command = new SubjectSelectsQuery
        {
            SubjectIds = evt.SubjectIds,
        };
        
        var response = await mediator.Send(command);
        
        await context.RespondAsync(response);
    }
}