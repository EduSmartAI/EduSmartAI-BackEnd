using BuildingBlocks.Messaging.Events.QuizService.MajorSelectsEvents;

namespace Course.Application.Majors.Queries;

public class MajorSelectsQueryHandler(IMajorService majorService) : IQueryHandler<MajorCodeSelectsQuery, MajorSelectsEventResponse>
{
    /// <summary>
    /// Handle MajorSelectsQuery to get a list of major names based on provided IDs.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<MajorSelectsEventResponse> Handle(MajorCodeSelectsQuery request, CancellationToken cancellationToken)
    {
        return await majorService.SelectMajorsAsync(request, cancellationToken);
    }
}