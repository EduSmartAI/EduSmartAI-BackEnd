using BaseService.Application.Interfaces.Repositories;
using MassTransit;

namespace QuizService.Application.Applications.QuizCourses.Consumers;

public class StudentQuizCourseInsertEventConsumer(IUnitOfWork unitOfWork) : IConsumer<StudentQuizCourseInsertEvent>
{
    public async Task Consume(ConsumeContext<StudentQuizCourseInsertEvent> context)
    {
        var evt = context.Message;
        
        unitOfWork.Store(evt.StudentQuiz);
        await unitOfWork.SessionSaveChangesAsync();
    }
}