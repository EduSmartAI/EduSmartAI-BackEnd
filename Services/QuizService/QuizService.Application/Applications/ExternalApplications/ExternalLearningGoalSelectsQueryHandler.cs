using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.QuizService.LearningGoalSelectsEvents;
using MassTransit;

namespace QuizService.Application.Applications.ExternalApplications;

public class ExternalLearningGoalSelectsQueryHandler(IRequestClient<LearningGoalSelectsEvent> requestLearningGoalSelectsClient) : IQueryHandler<ExternalLearningGoalSelectsQuery, LearningGoalSelectsEventResponse>
{
    public async Task<LearningGoalSelectsEventResponse> Handle(ExternalLearningGoalSelectsQuery request, CancellationToken cancellationToken)
    {
        var response = new LearningGoalSelectsEventResponse { Success = false};

        // Send message to StudentService to get LearningGoalSelects
        var messageLearningGoalSelects = await requestLearningGoalSelectsClient.GetResponse<LearningGoalSelectsEventResponse>(request, cancellationToken);
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