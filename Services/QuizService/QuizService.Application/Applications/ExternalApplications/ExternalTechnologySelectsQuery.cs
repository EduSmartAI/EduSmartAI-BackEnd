using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.QuizService;
using BuildingBlocks.Messaging.Events.QuizService.TechnologySelectsEvents;

namespace QuizService.Application.Applications.ExternalApplications;

public class ExternalTechnologySelectsQuery : IQuery<TechnologySelectsEventResponse>
{
    
}