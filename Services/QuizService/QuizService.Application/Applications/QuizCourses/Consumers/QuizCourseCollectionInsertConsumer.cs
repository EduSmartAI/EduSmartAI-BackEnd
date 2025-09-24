using BaseService.Application.Interfaces.Repositories;
using MassTransit;

namespace QuizService.Application.Applications.QuizCourses.Consumers;

public class QuizCourseCollectionInsertConsumer(IUnitOfWork unitOfWork) : IConsumer<QuizCourseCollectionInsertEvent>
{
    public async Task Consume(ConsumeContext<QuizCourseCollectionInsertEvent> context)
    {
        var evt = context.Message;
        
        unitOfWork.Store(evt.Quiz);
        await unitOfWork.SessionSaveChangesAsync();
    }
}