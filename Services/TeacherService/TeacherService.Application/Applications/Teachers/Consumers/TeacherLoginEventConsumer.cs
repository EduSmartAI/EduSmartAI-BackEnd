using BuildingBlocks.Messaging.Events.AuthService.UserLoginEvents;
using BuildingBlocks.Messaging.Events.UserLoginEvents;
using MassTransit;
using MediatR;
using TeacherService.Application.Applications.Teachers.Queries.Logins;

namespace TeacherService.Application.Applications.Teachers.Consumers;

public class TeacherLoginEventConsumer(IMediator mediator) : IConsumer<TeacherLoginEvent>
{
    public async Task Consume(ConsumeContext<TeacherLoginEvent> context)
    {
        var evt = context.Message;
        
        var query = new TeacherLoginQuery
        {
            UserId = evt.UserId
        };
            
        var response = await mediator.Send(query);
        
        await context.RespondAsync(response);
    }
}
