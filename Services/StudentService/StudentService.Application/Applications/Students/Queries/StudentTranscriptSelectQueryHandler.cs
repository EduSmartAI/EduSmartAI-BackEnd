using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.Students.Queries;

public class StudentTranscriptSelectQueryHandler(IStudentService studentService) : IQueryHandler<StudentTranscriptSelectQuery, StudentTranscriptSelectResponse>
{
    public async Task<StudentTranscriptSelectResponse> Handle(StudentTranscriptSelectQuery request, CancellationToken cancellationToken)
    {
        return await studentService.SelectStudentTranscriptAsync(request, cancellationToken);
    }
}