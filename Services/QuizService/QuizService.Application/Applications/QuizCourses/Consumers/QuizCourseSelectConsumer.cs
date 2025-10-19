using BuildingBlocks.Messaging.Events.CourseService.QuizCourseSelectEvents;
using MassTransit;
using MassTransit.Initializers;
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

        var response = await quizCourseService.SelectCourseQuiz(request)
            .Select(x => new QuizCourseSelectEventResponse
            {
                Success = x.Success,
                Message = x.Message,
                Response = new QuizCourseSelectEventResponseEntity
                {
                    QuizId = x.Response.QuizId,
                    DurationMinutes = x.Response.DurationMinutes,
                    PassingScorePercentage = x.Response.PassingScorePercentage,
                    ShuffleQuestions = x.Response.ShuffleQuestions,
                    ShowResultsImmediately = x.Response.ShowResultsImmediately,
                    AllowRetake = x.Response.AllowRetake,
                    Questions = x.Response.Questions
                        .Select(q => new BuildingBlocks.Messaging.Events.CourseService.QuizCourseSelectEvents.QuestionDetailResponse
                        {
                            QuestionId = q.QuestionId,
                            QuestionText = q.QuestionText,
                            QuestionType = q.QuestionType,
                            Explanation = q.Explanation,
                            Answers = q.Answers
                                .Select(a => new BuildingBlocks.Messaging.Events.CourseService.QuizCourseSelectEvents.AnswerDetailResponse
                                {
                                    AnswerId = a.AnswerId,
                                    AnswerText = a.AnswerText,
                                    IsCorrect = a.IsCorrect
								}).ToList()
                        }).ToList()
                },
                DetailErrors = x.DetailErrors,
                MessageId = x.MessageId
            });
        
       
        await context.RespondAsync(response);
    }
}