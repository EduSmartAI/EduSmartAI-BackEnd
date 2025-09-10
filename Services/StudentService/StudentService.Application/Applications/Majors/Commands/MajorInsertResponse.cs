using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.Majors.Commands;

public record MajorInsertResponse : AbstractApiResponse<string>
{
    public override string? Response { get; set; }
}