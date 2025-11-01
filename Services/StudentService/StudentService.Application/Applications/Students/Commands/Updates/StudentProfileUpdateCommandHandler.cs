using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.Students.Commands.Updates;

public class StudentProfileUpdateCommandHandler(IStudentService studentService) : ICommandHandler<StudentProfileUpdateCommand, StudentProfileUpdateResponse>
{
    public async Task<StudentProfileUpdateResponse> Handle(StudentProfileUpdateCommand request, CancellationToken cancellationToken)
    {
        return await studentService.UpdateStudentProfileAsync(request, cancellationToken);
    }
}