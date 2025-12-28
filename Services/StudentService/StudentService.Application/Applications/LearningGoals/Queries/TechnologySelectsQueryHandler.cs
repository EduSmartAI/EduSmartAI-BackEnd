using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.QuizService;
using BuildingBlocks.Messaging.Events.QuizService.TechnologySelectsEvents;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.LearningGoals.Queries;

public class TechnologySelectsQueryHandler(ITechnologyService technologyService) : IQueryHandler<TechnologySelectsQuery, TechnologySelectsEventResponse>
{
    /// <summary>
    /// Handles the TechnologySelectsQuery to retrieve technology selections.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TechnologySelectsEventResponse> Handle(TechnologySelectsQuery request, CancellationToken cancellationToken)
    {
        return await technologyService.SelectTechnologiesAsync(request);
    }
}