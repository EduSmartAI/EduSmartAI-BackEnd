using BuildingBlocks.Messaging.Events.QuizService;
using MassTransit;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Consumers;

public class StudentInformationSelectsEventConsumer(IStudentService studentService) : IConsumer<StudentInformationSelectsEvent>
{
    public async Task Consume(ConsumeContext<StudentInformationSelectsEvent> context)
    {
        var evt = context.Message;
        
        var response = await studentService.GetStudentInformationSelectsAsync(evt, context.CancellationToken);
        await context.RespondAsync(response);
    }
}