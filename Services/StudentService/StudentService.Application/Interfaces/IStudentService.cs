using BuildingBlocks.Messaging.Events.InsertUserEvents;
using StudentService.Application.Applications.Students.Commands.Inserts;
using StudentService.Domain.WriteModels;

namespace StudentService.Application.Interfaces;

public interface IStudentService
{
    Task<UserInsertEventResponse> InsertStudentAsync(StudentInsertCommand request, CancellationToken cancellationToken = default);
    
    Task<StudentInsertProfileResponse> InsertStudentProfileAsync(StudentInsertProfileCommand request, CancellationToken cancellationToken);
}