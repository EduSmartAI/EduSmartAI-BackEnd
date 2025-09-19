using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.QuizService.SubjectSelectEvents;

namespace Course.Application.Subjects.Queries;

public class SubjectSelectsQuery : IQuery<SubjectSelectsEventResponse>
{
    public List<Guid> SubjectIds { get; set; }
}