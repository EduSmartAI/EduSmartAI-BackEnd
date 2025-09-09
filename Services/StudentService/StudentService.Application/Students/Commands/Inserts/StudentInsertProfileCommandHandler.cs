using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Students.Commands.Inserts;

public class StudentInsertProfileCommandHandler : ICommandHandler<StudentInsertProfileCommand, StudentInsertProfileResponse>
{
    private readonly IStudentService _studentService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="studentService"></param>
    public StudentInsertProfileCommandHandler(IStudentService studentService)
    {
        _studentService = studentService;
    }

    /// <summary>
    /// Handle insert student profile command
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<StudentInsertProfileResponse> Handle(StudentInsertProfileCommand request, CancellationToken cancellationToken)
    {
        return await _studentService.InsertStudentProfileAsync(request, cancellationToken);
    }
}