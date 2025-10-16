using BuildingBlocks.CQRS;
using MediatR;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.QuizCourses.Commands;

public class QuizCourseAddQuestionsCommandHandler : ICommandHandler<QuizCourseAddQuestionsCommand, QuizCourseAddQuestionsResponse>
{
    private readonly IQuizCourseService _quizCourseService;

    public QuizCourseAddQuestionsCommandHandler(IQuizCourseService quizCourseService)
    {
        _quizCourseService = quizCourseService;
    }

    public async Task<QuizCourseAddQuestionsResponse> Handle(QuizCourseAddQuestionsCommand request, CancellationToken cancellationToken)
    {
        return await _quizCourseService.InsertQuestionsToQuizAsync(request, cancellationToken);
    }
}

