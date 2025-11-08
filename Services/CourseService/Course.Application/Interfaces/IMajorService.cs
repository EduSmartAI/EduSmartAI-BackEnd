using BuildingBlocks.Messaging.Events.QuizService.MajorSelectsEvents;
using Course.Application.Majors.Commands.CreateMajor;
using Course.Application.Majors.Queries;

namespace Course.Application.Interfaces;

public interface IMajorService
{
    /// <summary>
    /// Select major name by ID - original method
    /// </summary>
    /// <param name="id">Major identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Major name</returns>
    Task<string> SelectMajorAsync(Guid id, CancellationToken cancellationToken);

    Task<MajorSelectsEventResponse> SelectMajorsAsync(MajorCodeSelectsQuery request, CancellationToken cancellationToken);

    Task<CreateMajorResponse> CreateMajorAsync(CreateMajorCommand request, CancellationToken cancellationToken);
}