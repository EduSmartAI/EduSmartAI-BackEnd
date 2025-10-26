using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.AuthService.UserLoginEvents;
using BuildingBlocks.Messaging.Events.UserLoginEvents;

namespace TeacherService.Application.Applications.Teachers.Queries.Logins;

public record TeacherLoginQuery : IQuery<TeacherLoginEventResponse>
{
    public Guid UserId { get; set; }
}