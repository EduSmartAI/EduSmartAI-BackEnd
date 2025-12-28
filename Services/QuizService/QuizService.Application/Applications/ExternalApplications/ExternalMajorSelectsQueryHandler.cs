using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.QuizService;
using BuildingBlocks.Messaging.Events.QuizService.MajorSelectsEvents;
using MassTransit;

namespace QuizService.Application.Applications.ExternalApplications;

public class ExternalMajorSelectsQueryHandler(IRequestClient<MajorCodeSelectsEvent> requestMajorSelectsEventClient) : IQueryHandler<ExternalMajorSelectsQuery, MajorSelectsEventResponse>
{
    public async Task<MajorSelectsEventResponse> Handle(ExternalMajorSelectsQuery request, CancellationToken cancellationToken)
    {
        var response = new MajorSelectsEventResponse { Success = false};

        // Send message to CourseService to get MajorSelects
        var messageMajorSelects = await requestMajorSelectsEventClient.GetResponse<MajorSelectsEventResponse>(request, cancellationToken);
        if (!messageMajorSelects.Message.Success)
        {
            response.MessageId = messageMajorSelects.Message.MessageId;
            response.Message = messageMajorSelects.Message.Message;
            return response;
        }

        // True
        response = messageMajorSelects.Message;
        return response;
    }
}