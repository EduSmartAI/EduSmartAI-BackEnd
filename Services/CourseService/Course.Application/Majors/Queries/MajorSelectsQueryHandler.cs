using BuildingBlocks.Messaging.Events.QuizService.MajorSelectsEvents;

namespace Course.Application.Majors.Queries;

public class MajorSelectsQueryHandler(IMajorService majorService) : IQueryHandler<MajorSelectsQuery, MajorSelectsEventResponse>
{
    /// <summary>
    /// Handle MajorSelectsQuery to get a list of major names based on provided IDs.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<MajorSelectsEventResponse> Handle(MajorSelectsQuery request, CancellationToken cancellationToken)
    {
        return await majorService.SelectMajorsAsync(request, cancellationToken);
    }
}