using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.Students.Commands.Updates;

public class StudentSemesterUpdateCommand : ICommand<StudentSemesterUpdateCommandResponse>
{
    public Guid SemesterId { get; set; }
}