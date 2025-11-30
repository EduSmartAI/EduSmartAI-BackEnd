using BuildingBlocks.Messaging.Events.QuizService;

namespace Course.Application.Subjects.Queries.SelectSubject;

public class SubjectSelectsQuery : IQuery<SubjectSelectsEventResponse>
{
    public List<Guid> SubjectIds { get; set; }
}