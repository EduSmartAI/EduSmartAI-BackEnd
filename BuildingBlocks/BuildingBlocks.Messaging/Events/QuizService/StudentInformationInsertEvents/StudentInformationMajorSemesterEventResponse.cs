using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.StudentInformationInsertEvents;

public record StudentInformationMajorSemesterEventResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}