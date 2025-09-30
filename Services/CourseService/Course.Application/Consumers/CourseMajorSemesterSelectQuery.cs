using BuildingBlocks.Messaging.Events.QuizService;

namespace Course.Application.Consumers;

public class CourseMajorSemesterSelectQuery : IQuery<CourseMajorSemesterSelectEventResponse>
{
    public Guid SemesterId { get; set; }
    
    public Guid MajorId { get; set; }
}