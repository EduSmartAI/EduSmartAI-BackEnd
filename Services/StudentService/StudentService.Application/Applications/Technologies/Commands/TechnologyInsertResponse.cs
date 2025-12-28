using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.Technologies.Commands;

public record TechnologyInsertResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}