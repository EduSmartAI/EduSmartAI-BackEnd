using BuildingBlocks.Messaging.Events.CourseService.QuizCourseInsertEvents;
using MassTransit;
using MassTransit.Initializers;
using QuizService.Application.Applications.QuizCourses.Commands;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.QuizCourses.Consumers;

public class QuizCourseInsertConsumer(IQuizCourseService quizCourseService) : IConsumer<QuizCourseInsertEvent>
{
    public async Task Consume(ConsumeContext<QuizCourseInsertEvent> context)
    {
        var evt = context.Message;
        
        var request = new QuizCourseInsertCommand
        {
            UserEmail = evt.UserEmail,
            DurationMinutes = evt.DurationMinutes,
            PassingScorePercentage = evt.PassingScorePercentage,
            ShuffleQuestions = evt.ShuffleQuestions,
            ShowResultsImmediately = evt.ShowResultsImmediately,
            AllowRetake = evt.AllowRetake,
            Questions = evt.Questions
                .Select(q => new QuizService.Application.Applications.QuizCourses.Commands.QuizCourseQuestionsInsert
                {
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    Explanation = q.Explanation,
                    Answers = q.Answers
                        .Select(a => new QuizService.Application.Applications.QuizCourses.Commands.QuizCourseAnswersInsert
                        {
                            AnswerText = a.AnswerText,
                            IsCorrect = a.IsCorrect
                        }).ToList()
                }).ToList()
        };

        var response = await quizCourseService.InsertQuizCourseAsync(request)
            .Select(x => new QuizCourseInsertEventResponse
            {
                Success = x.Success,
                Message = x.Message,
                Response = new QuizCourseInsertEventResponseEntity { QuizId = x.Response.QuizId},
                DetailErrors = x.DetailErrors,
                MessageId = x.MessageId
            });
        
        await context.RespondAsync(response);
    }
}