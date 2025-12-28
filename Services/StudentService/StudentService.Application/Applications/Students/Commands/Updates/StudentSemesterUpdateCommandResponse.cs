using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.Students.Commands.Updates;

public record StudentSemesterUpdateCommandResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}