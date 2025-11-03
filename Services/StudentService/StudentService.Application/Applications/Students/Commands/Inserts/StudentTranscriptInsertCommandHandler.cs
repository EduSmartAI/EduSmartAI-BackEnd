using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.Students.Commands.Inserts;

public class StudentTranscriptInsertCommandHandler(IStudentService studentService) : ICommandHandler<StudentTranscriptInsertCommand, StudentTranscriptInsertResponse>
{
    public async Task<StudentTranscriptInsertResponse> Handle(StudentTranscriptInsertCommand request, CancellationToken cancellationToken)
    {
        return await studentService.InsertStudentTranscriptAsync(request, cancellationToken);
    }
}