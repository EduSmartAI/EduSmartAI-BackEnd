using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.QuizCourses.Commands;

public class StudentQuizCourseInsertCommandHandler(IQuizCourseService quizCourseService) : ICommandHandler<StudentQuizCourseInsertCommand, StudentQuizCourseInsertResponse>
{
    public async Task<StudentQuizCourseInsertResponse> Handle(StudentQuizCourseInsertCommand request, CancellationToken cancellationToken)
    {
        return await quizCourseService.InsertStudentQuizCourseAsync(request, cancellationToken);
    }
}