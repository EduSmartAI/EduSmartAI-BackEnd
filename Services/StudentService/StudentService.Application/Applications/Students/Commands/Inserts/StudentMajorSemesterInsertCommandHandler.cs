using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.StudentInformationInsertEvents;
using MediatR;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.Students.Commands.Inserts;

public class StudentMajorSemesterInsertCommandHandler : ICommandHandler<StudentMajorSemesterInsertCommand, StudentInformationMajorSemesterEventResponse>
{
    private readonly IStudentService _studentService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="studentService"></param>
    public StudentMajorSemesterInsertCommandHandler(IStudentService studentService)
    {
        _studentService = studentService;
    }

    /// <summary>
    /// Hande 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<StudentInformationMajorSemesterEventResponse> Handle(StudentMajorSemesterInsertCommand request, CancellationToken cancellationToken)
    {
        return await _studentService.InsertStudentMajorSemesterInformationAsync(request, cancellationToken);
    }
}