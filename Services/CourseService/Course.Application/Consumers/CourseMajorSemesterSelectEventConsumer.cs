using BuildingBlocks.Messaging.Events.QuizService.CourseMajorSemesterSelectEvents;
using MassTransit;
using MediatR;

namespace Course.Application.Consumers;

public class CourseMajorSemesterSelectEventConsumer(IMediator mediator) : IConsumer<CourseMajorSemesterSelectEvent>
{
    /// <summary>
    /// Consumes the CourseMajorSemesterSelectEvent and processes it using MediatR to handle the associated query.
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<CourseMajorSemesterSelectEvent> context)
    {
        var message = context.Message;
        
        var query = new CourseMajorSemesterSelectQuery
        {
            MajorId = message.MajorId,
            SemesterId = message.SemesterId
        };
        
        var response = await  mediator.Send(query, context.CancellationToken);
        
        await context.RespondAsync(response);
    }
}