using BuildingBlocks.Messaging.Events.InsertUserEvents;
using BuildingBlocks.Messaging.Events.QuizService;
using StudentService.Application.Applications.Students.Commands.Inserts;
using StudentService.Domain.WriteModels;

namespace StudentService.Application.Interfaces;

public interface IStudentService
{
    Task<UserInsertEventResponse> InsertStudentAsync(StudentInsertCommand request, CancellationToken cancellationToken = default);
    
    Task<StudentInformationMajorSemesterEventResponse> InsertStudentMajorSemesterInformationAsync(StudentMajorSemesterInsertCommand request, CancellationToken cancellationToken);
}