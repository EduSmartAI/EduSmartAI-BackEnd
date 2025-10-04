using BuildingBlocks.Messaging.Events.AIService.InsertLearningPathEvent;
using MassTransit;
using StudentService.Application.Applications.LearningPaths.Commands;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Consumers
{
    public class InsertLearningPathConsumer(ILearningPathService service) : IConsumer<InsertLearningPathEvent>
    {
        public async Task Consume(ConsumeContext<InsertLearningPathEvent> context)
        {
            var evt = context.Message;
            Console.WriteLine("InsertLearningPathConsumer received event for LearningPathId: " + evt.LearningPathId);

            var request = new LearningPathInsertCommand
            {
                PathId = evt.LearningPathId,
                StudentEmail = evt.CurrentUserEmail,
                StudentId = evt.StudentId,
                PathName = evt.PathName,
            };

            await service.InsertLearningPathAsync(request, context.CancellationToken);
        }
    }
}
