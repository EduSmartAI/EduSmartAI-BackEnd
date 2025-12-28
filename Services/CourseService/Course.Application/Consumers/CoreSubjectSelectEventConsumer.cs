using BuildingBlocks.Messaging.Events.CourseService;
using MassTransit;
using MediatR;

namespace Course.Application.Consumers;

public class CoreSubjectSelectEventConsumer(IMediator mediator) : IConsumer<CoreSubjectSelectEvent>
{
    public async Task Consume(ConsumeContext<CoreSubjectSelectEvent> context)
    {
        var evt = context.Message;
        
        var query = new CoreSubjectSelectQuery
        {
            SubjectCodes = evt.SubjectCodes
        };
        
        var response = await mediator.Send(query);
        
        await context.RespondAsync(response);
    }
}

