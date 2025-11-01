using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.QuizService;
using BuildingBlocks.Messaging.Events.QuizService.TechnologySelectsEvents;
using MassTransit;

namespace QuizService.Application.Applications.ExternalApplications;

public class ExternalTechnologySelectsQueryHandler(IRequestClient<TechnologySelectsEvent> requestTechnologySelectsClient) : IQueryHandler<ExternalTechnologySelectsQuery, TechnologySelectsEventResponse>
{
    public async Task<TechnologySelectsEventResponse> Handle(ExternalTechnologySelectsQuery request, CancellationToken cancellationToken)
    {
        var response = new TechnologySelectsEventResponse { Success = false};

        // Send message to StudentService to get TechnologySelects
        var messageTechnologySelects = await requestTechnologySelectsClient.GetResponse<TechnologySelectsEventResponse>(request, cancellationToken);
        if (!messageTechnologySelects.Message.Success)
        {
            response.MessageId = messageTechnologySelects.Message.MessageId;
            response.Message = messageTechnologySelects.Message.Message;
            return response;
        }

        // True
        response = messageTechnologySelects.Message;
        return response;
    }
}