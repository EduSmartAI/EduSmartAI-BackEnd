using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.QuizCourses.Commands;

public class QuizCourseUpdateCommandHandler(IQuizCourseService quizCourseService) : ICommandHandler<QuizCourseUpdateCommand, QuizCourseUpdateResponse>
{
    public async Task<QuizCourseUpdateResponse> Handle(QuizCourseUpdateCommand request, CancellationToken cancellationToken)
    {
        return await quizCourseService.UpdateQuizCourseAsync(request, cancellationToken);
    }
}