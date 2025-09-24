using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.QuizService.StudentInformationInsertEvents;

public record StudentInformationMajorSemesterEventResponse : AbstractApiResponse<List<StudentMajorOrientation>>
{
    public override List<StudentMajorOrientation> Response { get; set; }
}