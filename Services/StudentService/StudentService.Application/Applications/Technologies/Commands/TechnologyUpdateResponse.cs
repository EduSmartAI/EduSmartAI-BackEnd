using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.Technologies.Commands;

public record TechnologyUpdateResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; } = null!;
}

