using BuildingBlocks.Messaging.Events.AuthService.InsertUserEvents;
using TeacherService.Application.Applications.Teachers.Commands.Inserts;

namespace TeacherService.Application.Interfaces;

public interface ITeacherService
{
    Task<LecturerInsertEventResponse> InsertTeacherAsync(LecturerInsertCommand request, CancellationToken cancellationToken = default);
}