using BuildingBlocks.Messaging.Events.AuthService.InsertUserEvents;
using MassTransit;
using MediatR;
using TeacherService.Application.Applications.Teachers.Commands.Inserts;

namespace TeacherService.Application.Applications.Teachers.Consumers;

public class LecturerInsertEventConsumer(IMediator mediator) : IConsumer<LecturerInsertEvent>
{
    public async Task Consume(ConsumeContext<LecturerInsertEvent> context)
    {
        var evt = context.Message;
        
        var command = new LecturerInsertCommand
        {
            UserId = evt.UserId,
            OldUserId = evt.OldUserId,
            Email = evt.Email,
            FirstName = evt.FirstName,
            LastName = evt.LastName,
        };
        await mediator.Send(command);
    }
}

