using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.CourseMajorSemesterSelectEvents;

namespace Course.Application.Consumers;

public class CourseMajorSemesterSelectQuery : IQuery<CourseMajorSemesterSelectEventResponse>
{
    public Guid SemesterId { get; set; }
    
    public Guid MajorId { get; set; }
}