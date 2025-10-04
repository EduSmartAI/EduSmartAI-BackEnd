using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.QuizService;

public record StudentInformationMajorSemesterEventResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}