using BuildingBlocks.Messaging.Events.AIService.AiChatLearningPathEvents;
using MassTransit;
using MediatR;
using StudentService.Application.Applications.LearningPaths.Commands;

namespace StudentService.Application.Consumers.AiChatLearningPath;

public class AiRegenerateLearningPathConsumer(ISender sender) : IConsumer<AiRegenerateLearningPath>
{
    public async Task Consume(ConsumeContext<AiRegenerateLearningPath> context)
    {
        var msg = context.Message;

        var cmd = new RegenerateLearningPathCommand
        {
            StudentId = msg.UserId,
            StudentEmail = msg.Email
        };

        var result = await sender.Send(cmd, context.CancellationToken);

        var response = new AiRegenerateLearningPathResponse
        {
            Success = result.Success,
            MessageId = result.MessageId,
            Message = result.Message,
            DetailErrors = result.DetailErrors,
            Response = result.Response
        };

        await context.RespondAsync(response);
    }
}


