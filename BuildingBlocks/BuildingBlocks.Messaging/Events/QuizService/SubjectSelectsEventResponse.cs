using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.QuizService;

public record SubjectSelectsEventResponse : AbstractApiResponse<List<SubjectSelectEventResponseEntity>>
{
    public override List<SubjectSelectEventResponseEntity> Response { get; set; }
}

public class SubjectSelectEventResponseEntity
{
    public Guid SubjectId { get; set; }
    
    public string SubjectNameCode { get; set; }
    public string SubjectName { get; set; }
    
    public string SubjectCode { get; set; }
}