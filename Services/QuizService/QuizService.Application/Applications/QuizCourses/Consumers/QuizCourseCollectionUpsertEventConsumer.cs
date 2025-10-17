using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using MassTransit;

namespace QuizService.Application.Applications.QuizCourses.Consumers;

public class QuizCourseCollectionUpsertEventConsumer(IUnitOfWork unitOfWork) : IConsumer<QuizCourseCollectionUpsertEvent>
{
    public async Task Consume(ConsumeContext<QuizCourseCollectionUpsertEvent> context)
    {
        var evt = context.Message;
        
        unitOfWork.Store(evt.Quiz);
        await unitOfWork.SessionSaveChangesAsync();
        
        // Clear cache
        var cacheKey = CacheKey.QuizCourses(evt.Quiz.QuizId);
        await unitOfWork.CacheRemoveAsync(cacheKey);
    }
}