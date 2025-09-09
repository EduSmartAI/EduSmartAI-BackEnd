using BaseService.Common.ApiEntities;

namespace StudentService.Application.Majors.Commands;

public record MajorInsertResponse : AbstractApiResponse<string>
{
    public override string? Response { get; set; }
}