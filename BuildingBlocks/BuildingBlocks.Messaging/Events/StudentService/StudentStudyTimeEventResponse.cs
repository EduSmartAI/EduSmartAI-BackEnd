using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.StudentService;

public record StudentStudyTimeEventResponse : AbstractApiResponse<int>
{
    public override int Response { get; set; }
}
