using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.UserBehaviours.Commands;

public record UserBehaviourInsertResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; } = null!;
}