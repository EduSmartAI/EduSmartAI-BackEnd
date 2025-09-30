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
                PathId = evt.LearningPathId,
                StudentEmail = evt.CurrentUserEmail,
                StudentId = evt.StudentId,
                PathName = evt.PathName,
            };

            await _service.InsertLearningPathAsync(request, context.CancellationToken);
        }
    }
}
