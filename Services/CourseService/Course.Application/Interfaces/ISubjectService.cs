using BuildingBlocks.Messaging.Events.QuizService.SubjectSelectEvents;
using Course.Application.Subjects.Queries;

namespace Course.Application.Interfaces;

public interface ISubjectService
{
    Task<SubjectSelectsEventResponse> SelectSubject(SubjectSelectsQuery request, CancellationToken cancellationToken);
}