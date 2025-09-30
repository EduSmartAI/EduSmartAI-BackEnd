using BuildingBlocks.Messaging.Events.QuizService;

namespace Course.Application.Consumers;

public class CoursesSelectConsumer(ICourseService courseService) : IConsumer<CoursesSelectEvent>
{
    public async Task Consume(ConsumeContext<CoursesSelectEvent> context)
    {
        var evt = context.Message;
        
        var response = await courseService.GetCourseSelectsAsync(evt);
        
        await context.RespondAsync(response);
    }
}