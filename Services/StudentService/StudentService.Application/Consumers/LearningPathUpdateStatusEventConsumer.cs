using BuildingBlocks.Messaging.Events.AIService;
using MassTransit;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Consumers;

public class LearningPathUpdateStatusEventConsumer(ILearningPathService learningPathService) : IConsumer<LearningPathUpdateStatusEvent>
{
    public async Task Consume(ConsumeContext<LearningPathUpdateStatusEvent> context)
    {
        var evt = context.Message;

        await learningPathService.UpdateLearningPathStatusAsync(evt.LearningPathId, context.CancellationToken);
    }
}