using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.Students.Queries;

public class StudentTechnologyGoalSelectQueryHandler(IStudentService studentService) : IQueryHandler<StudentTechnologyGoalSelectQuery, StudentTechnologyGoalSelectResponse>
{
    public async Task<StudentTechnologyGoalSelectResponse> Handle(StudentTechnologyGoalSelectQuery request, CancellationToken cancellationToken)
    {
        return await studentService.SelectStudentTechnologyGoalAsync(request, cancellationToken);
    }
}

