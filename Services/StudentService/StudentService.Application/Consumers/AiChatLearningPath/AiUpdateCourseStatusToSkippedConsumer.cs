using BuildingBlocks.Messaging.Events.AIService.AiChatLearningPathEvents;
using MassTransit;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Consumers.AiChatLearningPath;

public class AiUpdateCourseStatusToSkippedConsumer(ILearningPathService learningPathService)
    : IConsumer<AiUpdateCourseStatusToSkipped>
{
    public async Task Consume(ConsumeContext<AiUpdateCourseStatusToSkipped> context)
    {
        var message = context.Message;

        var serviceResponse = await learningPathService.UpdateCourseStatusToSkippedBySubjectAsync(
            message.UserId,
            message.LearningPathId,
            message.SubjectCode,
            message.Email,
            context.CancellationToken);

        var response = new AiUpdateCourseStatusToSkippedResponse
        {
            Success = serviceResponse.Success,
            MessageId = serviceResponse.MessageId,
            Message = serviceResponse.Message,
            DetailErrors = serviceResponse.DetailErrors,
            Response = serviceResponse.Response
        };

        await context.RespondAsync(response);
    }
}

