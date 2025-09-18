using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.QuizService.SemesterSelectsEvents;
using MassTransit;

namespace QuizService.Application.Applications.ExternalApplications;

public class ExternalSemesterSelectsQueryHandler(IRequestClient<SemesterSelectsEvent> requestSemesterSelectsEventClient) : IQueryHandler<ExternalSemesterSelectsQuery, SemesterSelectsEventResponse>
{
    public async Task<SemesterSelectsEventResponse> Handle(ExternalSemesterSelectsQuery request, CancellationToken cancellationToken)
    {
        var response = new SemesterSelectsEventResponse { Success = false};

        // Send message to CourseService to get SemesterSelects
        var messageLearningGoalSelects = await requestSemesterSelectsEventClient.GetResponse<SemesterSelectsEventResponse>(request, cancellationToken);
        if (!messageLearningGoalSelects.Message.Success)
        {
            response.MessageId = messageLearningGoalSelects.Message.MessageId;
            response.Message = messageLearningGoalSelects.Message.Message;
            return response;
        }

        // True
        response = messageLearningGoalSelects.Message;
        return response;
    }
}