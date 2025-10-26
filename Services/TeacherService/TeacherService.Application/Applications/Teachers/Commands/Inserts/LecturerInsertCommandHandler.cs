using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.AuthService.InsertUserEvents;
using TeacherService.Application.Interfaces;

namespace TeacherService.Application.Applications.Teachers.Commands.Inserts;

public class LecturerInsertCommandHandler : ICommandHandler<LecturerInsertCommand, LecturerInsertEventResponse>
{
    private readonly ITeacherService _teacherService;

    public LecturerInsertCommandHandler(ITeacherService teacherService)
    {
        _teacherService = teacherService;
    }

    public async Task<LecturerInsertEventResponse> Handle(LecturerInsertCommand request, CancellationToken cancellationToken)
    {
        return await _teacherService.InsertTeacherAsync(request, cancellationToken);
    }
}

