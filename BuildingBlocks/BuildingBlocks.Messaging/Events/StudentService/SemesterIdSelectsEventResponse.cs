using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.StudentService;

public record SemesterIdSelectsEventResponse : AbstractApiResponse<List<SemesterIdSelectsEventResponseEntity>>
{
    public override List<SemesterIdSelectsEventResponseEntity> Response { get; set; }
}

public class SemesterIdSelectsEventResponseEntity
{
    public int SemesterNumber { get; set; }
    
    public Guid SemesterId { get; set; }
    
    public string SemesterName { get; set; } = null!;
}