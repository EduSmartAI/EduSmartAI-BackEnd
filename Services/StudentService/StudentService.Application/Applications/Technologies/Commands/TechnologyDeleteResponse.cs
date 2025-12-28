using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.Technologies.Commands;

public record TechnologyDeleteResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}


