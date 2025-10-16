using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.QuizCourses.Commands;

public class QuizCourseDeleteQuestionsCommandHandler : ICommandHandler<QuizCourseDeleteQuestionsCommand, QuizCourseDeleteQuestionsResponse>
{
    private readonly IQuizCourseService _quizCourseService;

    public QuizCourseDeleteQuestionsCommandHandler(IQuizCourseService quizCourseService)
    {
        _quizCourseService = quizCourseService;
    }

    public async Task<QuizCourseDeleteQuestionsResponse> Handle(QuizCourseDeleteQuestionsCommand request, CancellationToken cancellationToken)
    {
        return await _quizCourseService.DeleteQuestionsFromQuizAsync(request, cancellationToken);
    }
}

