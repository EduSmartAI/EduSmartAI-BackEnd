using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.Students.Commands.Updates;

public record StudentProfileUpdateResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}