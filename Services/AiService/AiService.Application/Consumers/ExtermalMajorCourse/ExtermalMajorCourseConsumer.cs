using AiService.Application.Features.AiExternalCourse;
using BuildingBlocks.Messaging.Events.AIService.InsertInternalExternalMajorEvent;
using MassTransit;
using MediatR;

namespace AiService.Application.Consumers.ExtermalMajorCourse
{
    public class ExtermalMajorCourseConsumer(IMediator mediator) : IConsumer<ExternalMajorEvent>
    {
        public async Task Consume(ConsumeContext<ExternalMajorEvent> context)
        {
            var evt = context.Message;
            foreach (var major in evt.Majors)
            {
                var request = new AiExternalCourseRequest
                {
                    GoalMajor = "Tôi muốn lộ trình về mảng IT và về " + major.Description
                };
                var response = await mediator.Send(request);
            }
        }
    }
}
