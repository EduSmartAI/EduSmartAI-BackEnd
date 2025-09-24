using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.QuizService.StudentTechnologyOrientationEvents;

public record StudentMajorOrientationEventResponse : AbstractApiResponse<StudentMajorOrientationEventResponseEntity>
{
    public override StudentMajorOrientationEventResponseEntity Response { get; set; } = null!;
}

public class StudentMajorOrientationEventResponseEntity
{
    public List<MajorInternal> MajorInternals { get; set; } = null!;
    
    public List<MajorExternal> MajorExternals { get; set; } = null!;
}

public class MajorInternal
{
    public string MajorName { get; set; } = null!;
    
    public string Reason { get; set; } = null!;
}

public class MajorExternal
{
    public string MajorName { get; set; } = null!;
    
    public string Reason { get; set; } = null!;
}