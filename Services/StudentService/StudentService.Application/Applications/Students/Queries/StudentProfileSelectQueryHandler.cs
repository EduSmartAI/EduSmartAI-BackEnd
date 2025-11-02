using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.Students.Queries;

public class StudentProfileSelectQueryHandler(IStudentService studentService) : IQueryHandler<StudentProfileSelectQuery, StudentProfileSelectResponse>
{
    public async Task<StudentProfileSelectResponse> Handle(StudentProfileSelectQuery request, CancellationToken cancellationToken)
    {
        return await studentService.SelectStudentProfileAsync(request, cancellationToken);
    }
}


