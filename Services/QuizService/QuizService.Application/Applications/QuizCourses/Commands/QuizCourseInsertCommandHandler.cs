using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.QuizCourses.Commands;

public class QuizCourseInsertCommandHandler(IQuizCourseService quizCourseService) : ICommandHandler<QuizCourseInsertCommand, QuizCourseInsertResponse>
{
    public async Task<QuizCourseInsertResponse> Handle(QuizCourseInsertCommand request, CancellationToken cancellationToken)
    {
        return await quizCourseService.InsertQuizCourseAsync(request);
    }
}