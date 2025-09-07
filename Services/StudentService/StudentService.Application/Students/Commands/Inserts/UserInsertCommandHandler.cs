using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.InsertUserEvents;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Students.Commands.Inserts;

public class UserInsertCommandHandler : ICommandHandler<UserInsertCommand, UserInsertEventResponse>
{
    private readonly IStudentService _studentService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="studentService"></param>
    public UserInsertCommandHandler(IStudentService studentService)
    {
        _studentService = studentService;
    }

    /// <summary>
    /// Handles user insertion by creating a new user record
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<UserInsertEventResponse> Handle(UserInsertCommand request, CancellationToken cancellationToken)
    {
        return await _studentService.InsertStudentAsync(request, cancellationToken);
    }
}