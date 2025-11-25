using BuildingBlocks.Messaging.Events.QuizService.SubjectSelectEvents;

namespace Course.Application.Subjects.Queries.SelectSubject;

public class SubjectSelectsQueryHandler(ISubjectService subjectService) : IQueryHandler<SubjectSelectsQuery, SubjectSelectsEventResponse>
{
    /// <summary>
    /// Handle select subject
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SubjectSelectsEventResponse> Handle(SubjectSelectsQuery request, CancellationToken cancellationToken)
    {
        return await subjectService.SelectSubject(request, cancellationToken);
    }
}