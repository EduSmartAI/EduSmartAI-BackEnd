using BuildingBlocks.Messaging.Events.QuizService.SemesterSelectsEvents;

namespace Course.Application.Semesters.Queries.SelectSemesters;

public class SemesterSelectsQueryHandler(ISemesterService semesterService) : IQueryHandler<SemesterSelectsQuery, SemesterSelectsEventResponse>
{
    /// <summary>
    /// Handle SemesterSelectsQuery to get a list of major names based on provided IDs.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SemesterSelectsEventResponse> Handle(SemesterSelectsQuery request, CancellationToken cancellationToken)
    {
        return await semesterService.SelectSemestersAsync(request, cancellationToken);
    }
}