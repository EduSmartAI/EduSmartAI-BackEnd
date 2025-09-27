using BuildingBlocks.Messaging.Events.QuizService.SemesterSelectsEvents;
using Course.Application.Semesters.Queries;

namespace Course.Application.Interfaces;

public interface ISemesterService
{
    Task<string> SelectSemesterAsync(Guid id, CancellationToken cancellationToken);
    
    Task<SemesterSelectsEventResponse> SelectSemestersAsync(SemesterSelectsQuery request, CancellationToken cancellationToken);
}