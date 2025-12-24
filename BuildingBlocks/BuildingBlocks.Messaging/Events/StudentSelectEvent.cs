using BaseService.Common.ApiEntities;
using BuildingBlocks.Messaging.Events.QuizService;

namespace BuildingBlocks.Messaging.Events;

public class StudentSelectEvent
{
    public Guid StudentId { get; set; }
}

public record StudentSelectEventResponse : AbstractApiResponse<StudentSelectEventResponseEntity>
{
    public override StudentSelectEventResponseEntity Response { get; set; }
}

public class StudentSelectEventResponseEntity
{
    public required string Name { get; set; }
    
    public required string Email { get; set; }
}