using BuildingBlocks.Messaging.Events.InsertUserEvents;
using StudentService.Application.Students.Commands.Inserts;
using StudentService.Domain.WriteModels;

namespace StudentService.Application.Interfaces;

public interface IStudentService
{
    Task<UserInsertEventResponse> InsertStudentAsync(UserInsertCommand request, CancellationToken cancellationToken = default);
    
    Task<(bool, Student?)> IsStudentExistAsync(Guid userId, CancellationToken cancellationToken = default);
}