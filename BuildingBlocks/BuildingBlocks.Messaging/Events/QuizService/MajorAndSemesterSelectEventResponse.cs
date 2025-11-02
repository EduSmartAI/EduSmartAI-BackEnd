using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.QuizService;

public record MajorAndSemesterSelectEventResponse : AbstractApiResponse<MajorAndSemesterSelectEventResponseEntity>
{
    public override MajorAndSemesterSelectEventResponseEntity Response { get; set; }
}

public class MajorAndSemesterSelectEventResponseEntity
{
    public MajorSelectEventResponseEntity? Major { get; set; }
    
    public SemesterSelectEventResponseEntity? Semester { get; set; }
}

public class SemesterSelectEventResponseEntity
{
    public Guid SemesterId { get; set; }
    
    public string SemesterName { get; set; } = null!;
}

public class MajorSelectEventResponseEntity
{
    public Guid MajorId { get; set; }
    
    public string MajorName { get; set; } = null!;
}

