using BaseService.Common.ApiEntities;

namespace StudentService.Application.Students.Commands.Inserts;

public record StudentInsertProfileResponse : AbstractApiResponse<string>
{
    public override string? Response { get; set; }
}