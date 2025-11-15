using BuildingBlocks.Messaging.Events.AuthService.InsertUserEvents;
using BuildingBlocks.Messaging.Events.QuizService;
using StudentService.Application.Applications.Students.Commands.Inserts;
using StudentService.Application.Applications.Students.Commands.Updates;
using StudentService.Application.Applications.Students.Queries;

namespace StudentService.Application.Interfaces;

public interface IStudentService
{
    Task<StudentInsertEventResponse> InsertStudentAsync(StudentInsertCommand request, CancellationToken cancellationToken = default);
    
    Task<StudentInformationMajorSemesterEventResponse> InsertStudentMajorSemesterInformationAsync(StudentMajorSemesterInsertCommand request, CancellationToken cancellationToken);
    
    Task<StudentInformationSelectsEventResponse> GetStudentInformationSelectsAsync(StudentInformationSelectsEvent request, CancellationToken cancellationToken = default);
    
    Task<StudentProfileUpdateResponse> UpdateStudentProfileAsync(StudentProfileUpdateCommand request, CancellationToken cancellationToken);
    
    Task<StudentProfileSelectResponse> SelectStudentProfileAsync(StudentProfileSelectQuery request, CancellationToken cancellationToken);
    
    Task<StudentTranscriptInsertResponse> InsertStudentTranscriptAsync(StudentTranscriptInsertCommand request, CancellationToken cancellationToken);
    
    Task<StudentTranscriptSelectResponse> SelectStudentTranscriptAsync(StudentTranscriptSelectQuery request, CancellationToken cancellationToken);
    
    Task<StudentTechnologyGoalSelectResponse> SelectStudentTechnologyGoalAsync(StudentTechnologyGoalSelectQuery request, CancellationToken cancellationToken);
}