using BuildingBlocks.Messaging.Events.QuizService.SubjectSelectEvents;
using Course.Application.Subjects.Commands.CreateSubject;
using Course.Application.Subjects.Queries;

namespace Course.Application.Interfaces;

public interface ISubjectService
{
    Task<SubjectSelectsEventResponse> SelectSubject(SubjectSelectsQuery request, CancellationToken cancellationToken);
    Task<CreateSubjectResponse> CreateSubjectAsync(CreateSubjectCommand request, CancellationToken cancellationToken);
}