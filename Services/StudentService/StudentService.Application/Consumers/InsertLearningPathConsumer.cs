using BuildingBlocks.Messaging.Events.AIService.InsertLearningPathEvent;
using MassTransit;
using StudentService.Application.Applications.LearningPaths.Commands;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Consumers
{
    public class InsertLearningPathConsumer(ILearningPathService _service) : IConsumer<InsertLearningPathEventLearningPathEvent>
    {
        public async Task Consume(ConsumeContext<InsertLearningPathEventLearningPathEvent> context)
        {
            var evt = context.Message;

            var request = new LearningPathInsertCommand
            {
                PathId = evt.LearningPathId
            };

            var res = await _service.InsertLearningPathAsync(request, context.CancellationToken);

            await context.RespondAsync(new InsertLearningPathResponseEvent(
                Success: res.Success
            ));
        }
    }
}
