using BaseService.Common.ApiEntities;
using BuildingBlocks.Messaging.Events.UserLoginEvents;

namespace BuildingBlocks.Messaging.Events.AuthService.UserLoginEvents;

public record TeacherLoginEventResponse : AbstractApiResponse<UserLoginEntity>
{
    public override UserLoginEntity Response { get; set; }
}

