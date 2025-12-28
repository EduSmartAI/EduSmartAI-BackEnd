using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.AuthService.UserLoginEvents;

public record StudentLoginEventResponse : AbstractApiResponse<UserLoginEntity>
{ 
    public override UserLoginEntity Response { get; set; } = null!;
}

public record UserLoginEntity(
    string FirstName,
    string LastName,
    string? AvatarUrl
);