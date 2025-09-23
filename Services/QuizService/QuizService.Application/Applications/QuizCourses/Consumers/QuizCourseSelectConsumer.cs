using BuildingBlocks.Messaging.Events.CourseService.QuizCourseSelectEvents;
using MassTransit;
using QuizService.Application.Applications.QuizCourses.Queries;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.QuizCourses.Consumers;

public class QuizCourseSelectConsumer(IQuizCourseService quizCourseService) : IConsumer<QuizCourseSelectEvent>
{
    public async Task Consume(ConsumeContext<QuizCourseSelectEvent> context)
    {
        var evt = context.Message;
        
        var request = new QuizCourseSelectQuery
        {
            QuizId = evt.QuizId
        };

        var response = await quizCourseService.SelectCourseQuiz(request);
        await context.RespondAsync(response);
    }
}