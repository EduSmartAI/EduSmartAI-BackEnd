using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.QuizService.SubjectSelectEvents;
using Course.Application.Interfaces;

namespace Course.Application.Subjects.Queries;

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