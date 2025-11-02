using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.StudentService;

public record AvatarUploadEventResponse : AbstractApiResponse<AvatarUploadEventResponseEntity>
{
    public override AvatarUploadEventResponseEntity Response { get; set; }
}

public class AvatarUploadEventResponseEntity
{
    public string AvatarUrl { get; set; }
}