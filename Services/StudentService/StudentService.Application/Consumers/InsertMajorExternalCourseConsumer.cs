using BuildingBlocks.Messaging.Events.AIService.UpdateExternalMajorEvent;
using MassTransit;
using StudentService.Application.Applications.LearningPathsMajor.Commands.InsertLearningPathsMajor;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Consumers
{
    public class InsertMajorExternalCourseConsumer(ILearningPathService _service) : IConsumer<UpdateExternalMajorEvent>
    {
        public async Task Consume(ConsumeContext<UpdateExternalMajorEvent> context)
        {
            var evt = context.Message;

            var request = new InsertLearningPathsMajorCommand
            {
                PathId = evt.LearningPathId,
                CurrentUserEmail = evt.CurrentUserEmail,
                MajorCode = evt.MajorCode,
                Reason = evt.Reason,
                Courses = evt.Steps,
            };

            var res = await _service.InsertLearningPathMajorCourseAsync(request, context.CancellationToken);
            var resp = new UpdateExternalMajorEventResponse
            {
                Success = res.Success,
                Response = res.Success ? "OK" : string.Empty,
                DetailErrors = []
            };
            await context.RespondAsync(resp);
        }
    }
}
