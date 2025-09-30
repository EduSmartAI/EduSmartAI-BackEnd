using BuildingBlocks.Messaging.Events.StudentService;
using MassTransit;
using QuizService.Application.Applications.StudentSurveys.Queries;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.Consumers;

public class StudentStudyTimeConsumer(IStudentSurveyService studentSurveyService) : IConsumer<StudentStudyTimeEvent>
{
    public async Task Consume(ConsumeContext<StudentStudyTimeEvent> context)
    {
        var evt = context.Message;
        
        var request = new StudentStudyTimeRequest
        {
            StudentId = evt.StudentId
        };
       
        var response = await studentSurveyService.GetStudentStudyTimeAsync(request, context.CancellationToken);
        
        await context.RespondAsync(new StudentStudyTimeEventResponse
        {
            Response = response.Response,
            Success = response.Success,
            Message = response.Message,
            DetailErrors = response.DetailErrors,
            MessageId = response.MessageId,
        });
    }
}