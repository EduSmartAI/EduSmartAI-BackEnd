using BaseService.Common.ApiEntities;
using BuildingBlocks.Pagination;

namespace StudentService.Application.Applications.UserBehaviours.Queries.SelectAllUserBehaviour;

/// <summary>
/// Response for getting all user behaviours
/// </summary>
public record SelectAllUserBehaviourResponse : AbstractApiResponse<List<UserBehaviourDto>>
{
    public override List<UserBehaviourDto> Response { get; set; } = null!;
}

/// <summary>
/// User behaviour DTO
/// </summary>
public class UserBehaviourDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string ActionType { get; set; } = null!;
    public Guid? TargetId { get; set; }
    public string? TargetType { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
}

