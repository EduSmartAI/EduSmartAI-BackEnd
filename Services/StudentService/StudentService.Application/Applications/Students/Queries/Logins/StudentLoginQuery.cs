using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.UserLoginEvents;

namespace StudentService.Application.Applications.Students.Queries.Logins;

public record StudentLoginQuery : IQuery<StudentLoginEventResponse>
{
    public Guid UserId { get; set; }
}